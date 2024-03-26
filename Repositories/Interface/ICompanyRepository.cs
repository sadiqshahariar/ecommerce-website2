using Awsome.Models;

namespace Awsome.Repositories.Interface
{
    public interface ICompanyRepository
    {
        List<Company> GetCompanyList();
        bool CreateCompany(Company obj);
        Company? FindComapny(int? id);
        bool UpdateCompany(Company obj);
        bool DeleteCompany(Company obj);
    }
}
