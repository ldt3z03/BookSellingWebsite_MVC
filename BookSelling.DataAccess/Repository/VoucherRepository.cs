using BookSelling.DataAccess.Data;
using BookSelling.DataAccess.Repository.IRepository;
using BookSelling.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookSelling.DataAccess.Repository
{
    public class VoucherRepository : Repository<Voucher>, IVoucherRepository
    {
        private readonly ApplicationDbContext _db;

        public VoucherRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Voucher obj)
        {
            _db.Attach(obj);
            _db.Entry(obj).State = EntityState.Modified;
        }
        public Voucher GetVoucherByCode(string code)
        {
            return _db.Vouchers.FirstOrDefault(v => v.Code == code && v.ExpiryDate > DateTime.Now);
        }

    }
}
