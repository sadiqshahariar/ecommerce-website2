using Awsome.Models;
using Awsome.Repositories.Interface;
using Awsome.Repositories.Repository;
using Awsome.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;

namespace Awsome.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
       private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment  _webHostEnvironment;

        public ProductController(IProductRepository productRepository, IWebHostEnvironment webHostEnvironment)
        {
            _productRepository = productRepository;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<NewProduct> objProductList = _productRepository.getProductList();
            return View(objProductList);
        }

        public IActionResult Create()
        {
            IEnumerable<SelectListItem> CategoryList = _productRepository.GetCategory().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            //ViewBag.CategoryList = CategoryList;

            ProductVM productVM = new()
            {
                CategoryList = CategoryList,
                Product = new NewProduct()

            };
            return View(productVM);
        }

        [HttpPost]
        public IActionResult Create(ProductVM obj,IFormFile file)
        {
            
            if (ModelState.IsValid)
            {
               /* string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"Images\Product");
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.Product.ImageUrl = @"Images\Product\" + fileName;
                }*/
                /*string webRootPath = _webHostEnvironment.WebRootPath;
                string logoFolderPath = Path.Combine(webRootPath, "productimages");

                string logoFolderPath2 = "productimages";
                // Create the logo folder if it doesn't exist
                Directory.CreateDirectory(logoFolderPath);

                // Combine with the filename to get the full path to save the picture
                string filePath = Path.Combine(logoFolderPath, file.FileName);

                string relativeFilePath = Path.Combine(logoFolderPath2, file.FileName);

                // Write the picture to the file using a FileStream
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyToAsync(fileStream);
                }
                obj.Product.ImageUrl = relativeFilePath;*/

                if(file!=null)
                {
                    string folder = "product/images/";
                    folder += Guid.NewGuid().ToString() + "_" + file.FileName;
                    string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);
                    file.CopyToAsync(new FileStream(serverFolder, FileMode.Create));
                    obj.Product.ImageUrl = folder;

				}
                
                bool ans = _productRepository.SaveData(obj.Product);
                TempData["success"] = "Product Created Successfully";
                return RedirectToAction("Index");
                
            }
            
            return View();


        }

        public IActionResult Edit(int id)
        {
            if (id == 0) return NotFound();
            NewProduct? productFromDB = _productRepository.Find(id);

            if (productFromDB == null) return NotFound();

            IEnumerable<SelectListItem> CategoryList = _productRepository.GetCategory().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            ProductVM productVM = new()
            {
                CategoryList = CategoryList,
                Product = new NewProduct()

            };
            productVM.Product = productFromDB;

            return View(productVM);
        }
        [HttpPost]
        public IActionResult Edit(ProductVM obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                if(file!=null)
                {


					//delete the old one picture
					if (!string.IsNullOrEmpty(obj.Product.ImageUrl))
					{
						// Parse folder path from the image URL
						string folderPath = obj.Product.ImageUrl.Substring(0, obj.Product.ImageUrl.LastIndexOf('/'));

						string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, folderPath);
						string oldImageFullPath = Path.Combine(oldImagePath, Path.GetFileName(obj.Product.ImageUrl));

						if (System.IO.File.Exists(oldImageFullPath))
						{
							System.IO.File.Delete(oldImageFullPath);
						}
					}

					string folder = "product/images/";
					folder += Guid.NewGuid().ToString() + "_" + file.FileName;
					string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);
					file.CopyToAsync(new FileStream(serverFolder, FileMode.Create));
					obj.Product.ImageUrl = folder;
				}

                bool ans = _productRepository.SaveUpdate(obj.Product);
                TempData["success"] = "Product Update Successfully";
                return RedirectToAction("Index");
            }
            return View();

        }

        public IActionResult Delete(int id)
        {
            if (id == 0) return NotFound();
            NewProduct? productFromDb = _productRepository.Find(id);
            if (productFromDb == null) return NotFound();
            return View(productFromDb);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(NewProduct obj1)
        {
            int id = obj1.Id;
            NewProduct? obj = _productRepository.Find(id);
            if (obj == null) return NotFound();

			if (!string.IsNullOrEmpty(obj1.ImageUrl))
			{
				// Parse folder path from the image URL
				string folderPath = obj1.ImageUrl.Substring(0, obj.ImageUrl.LastIndexOf('/'));

				string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, folderPath);
				string oldImageFullPath = Path.Combine(oldImagePath, Path.GetFileName(obj.ImageUrl));

				if (System.IO.File.Exists(oldImageFullPath))
				{
					System.IO.File.Delete(oldImageFullPath);
				}
			}

			bool and = _productRepository.DeleteData(obj);

            TempData["success"] = "Category Delete Successfully";
            return RedirectToAction("Index");


        }


        #region API CALLS 
        [HttpGet]
        public IActionResult GetAll()
        {
            List<NewProduct> objProductList = _productRepository.getProductList();
            return Json(new { data = objProductList });
        }
       #endregion
    }
}
