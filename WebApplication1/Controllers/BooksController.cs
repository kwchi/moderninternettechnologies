using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplicationData.Data;       // Для класу Book
using WebApplicationData.Interfaces; // Для репозиторію

using Microsoft.AspNetCore.Hosting;  // Для IWebHostEnvironment
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplicationData.Repositories;


namespace WebApplication1.Controllers
{
    public class BooksController : Controller
    {
        private readonly IWebRepository _repository;
        private readonly IWebHostEnvironment _webHostEnvironment; // Сервіс для роботи з файлами

        // Інжектуємо IWebHostEnvironment для збереження картинок
        public BooksController(IWebRepository repository, IWebHostEnvironment webHostEnvironment)
        {
            _repository = repository;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. Відображення списку (з пагінацією)
        public async Task<IActionResult> Index(int? pageNumber)
        {
            var booksQuery = _repository.ReadAll<Book>();

            // Змінимо розмір сторінки на 6, щоб картки гарно виглядали (2 ряди по 3)
            int pageSize = 6;

            // ВИПРАВЛЕНО: Використовуємо GetValueOrDefault(1)
            var paginatedBooks = await PaginatedList<Book>.CreateAsync(booksQuery, pageNumber.GetValueOrDefault(1), pageSize);

            return View(paginatedBooks);
        }

        // 2. Відображення форми створення
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 3. Обробка форми створення (з логікою завантаження файлу)
        [HttpPost]
        public async Task<IActionResult> Create(BookViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = null;

                // Перевіряємо, чи користувач вибрав фото
                if (model.CoverImage != null)
                {
                    // Визначаємо шлях до папки wwwroot/images/books
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "books");

                    // Створюємо папку, якщо її ще немає
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // Генеруємо унікальне ім'я файлу (GUID + оригінальне ім'я)
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.CoverImage.FileName;

                    // Повний шлях для збереження файлу
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Копіюємо файл на сервер
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CoverImage.CopyToAsync(fileStream);
                    }
                }

                // Створюємо сутність книги для БД
                var bookEntity = new Book
                {
                    Title = model.Title,
                    AuthorEmail = model.AuthorEmail,
                    Year = model.Year,
                    Price = model.Price,
                    Discount = model.Discount,
                    CoverImagePath = uniqueFileName
                };

                // Зберігаємо в базу даних
                await _repository.AddAsync(bookEntity);

                return RedirectToAction("Index");
            }

            // Якщо валідація не пройшла, повертаємо форму з помилками
            return View(model);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var book = await _repository.ReadAll<WebApplicationData.Data.Book>()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }
        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,AuthorEmail,Year,Price,Discount,CoverImagePath")] WebApplicationData.Data.Book book, IFormFile? CoverImage)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Логіка збереження картинки (якщо завантажили нову)
                    if (CoverImage != null)
                    {
                        // 1. Видаляємо стару (опціонально)
                        // 2. Зберігаємо нову
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(CoverImage.FileName);
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/books", fileName);

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await CoverImage.CopyToAsync(stream);
                        }

                        book.CoverImagePath = fileName;
                    }

                    // Оновлюємо запис через репозиторій
                    await _repository.UpdateAsync(book);
                }
                catch (Exception) // Тут можна додати DbUpdateConcurrencyException
                {
                    if (!await BookExists(book.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // Допоміжний метод для перевірки існування
        private async Task<bool> BookExists(int id)
        {
            var book = await _repository.ReadAll<WebApplicationData.Data.Book>()
                .FirstOrDefaultAsync(e => e.Id == id);
            return book != null;
        }

        // 4. Remote Validation: Перевірка назви
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> VerifyTitle(string title)
        {
            var exists = await _repository.ExistsAsync<Book>(b => b.Title == title);
            if (exists)
            {
                return Json($"Книга з назвою '{title}' вже існує.");
            }
            return Json(true);
        }

        // 5. Dependent Validation: Перевірка знижки
        [AcceptVerbs("GET", "POST")]
        public IActionResult VerifyDiscount(decimal discount, decimal price)
        {
            if (discount > price)
            {
                return Json("Знижка не може бути більшою за ціну товару.");
            }
            return Json(true);
        }
    }
}