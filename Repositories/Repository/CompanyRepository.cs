using Awsome.Data;
using Awsome.Models;
using Awsome.Repositories.Interface;

namespace Awsome.Repositories.Repository
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly ApplicationDbContext _db;
        public CompanyRepository(ApplicationDbContext db)
        {
            _db = db;

        }

        public bool CreateCompany(Company obj)
        {
            _db.Companies.Add(obj);
            _db.SaveChanges();
            return true;
        }

        public bool DeleteCompany(Company obj)
        {
            _db.Companies.Remove(obj);
            _db.SaveChanges();
            return true;
        }

        public Company? FindComapny(int? id)
        {
            return _db.Companies.Find(id);
        }

        public List<Company> GetCompanyList()
        {
            return _db.Companies.ToList();
        }

        public bool UpdateCompany(Company obj)
        {
            _db.Companies.Update(obj);
            _db.SaveChanges();
            return true;
        }
    }
}
