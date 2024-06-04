using BookSelling.Models;

namespace BookSelling.DataAccess.Repository.IRepository
{
    public interface IVoucherRepository : IRepository<Voucher>
    {
        Voucher GetVoucherByCode(string code);
        void Update(Voucher obj);

    }
}
