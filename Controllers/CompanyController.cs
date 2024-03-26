using Awsome.Models;
using Awsome.Repositories.Interface;
using Awsome.Repositories.Repository;
using Awsome.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Awsome.Controllers
{
	[Authorize(Roles = SD.Role_Admin)]
	public class CompanyController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ICompanyRepository _companyRepository;

        public CompanyController(IWebHostEnvironment webHostEnvironment, ICompanyRepository companyRepository)
        {
            _webHostEnvironment = webHostEnvironment;
            _companyRepository = companyRepository;
        }

        public IActionResult Index()
        {
            List<Company> objCompanyList = _companyRepository.GetCompanyList();
            return View(objCompanyList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Company obj)
        {
            if (ModelState.IsValid)
            {
                bool ans = _companyRepository.CreateCompany(obj);
                TempData["success"] = "Comapany Created Successfully";
                return RedirectToAction("Index");
            }
            return View();

        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            Company? categoryFromDB = _companyRepository.FindComapny(id);
            if (categoryFromDB == null) return NotFound();
            return View(categoryFromDB);
        }

        [HttpPost]
        public IActionResult Edit(Company obj)
        {
            if (ModelState.IsValid)
            {
                bool ans = _companyRepository.UpdateCompany(obj);
                TempData["success"] = "Category Update Successfully";
                return RedirectToAction("Index");
            }
            return View();

        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();
            Company? companyFromDB = _companyRepository.FindComapny(id);
            if (companyFromDB == null) return NotFound();
            return View(companyFromDB);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            Company? obj = _companyRepository.FindComapny(id);
            if (obj == null) return NotFound();
            bool ans = _companyRepository.DeleteCompany(obj);
            TempData["success"] = "Category Delete Successfully";
            return RedirectToAction("Index");


        }
    }
}
