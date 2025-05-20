using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Database;
using System.Data.Entity;
using Dealogikal.Utils;

namespace Dealogikal.Repository
{
    public class BaseRepository<T> : IBaseRepository<T>
        where T : class
    {
        public DbContext _db;
        public DbSet<T> _table;

        public BaseRepository()
        {
            _db = new DealogikalDTREntities();
            _table = _db.Set<T>();
        }
        public ErrorCode Create(T t, out string errorMsg)
        {
            try
            {
                _table.Add(t);
                _db.SaveChanges();
                errorMsg = "Success";

                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode Delete(object id, out string errorMsg)
        {
            try
            {
                var obj = Get(id);
                _table.Remove(obj);
                _db.SaveChanges();

                errorMsg = "Deleted";

                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return ErrorCode.Error;
            }
        }

        public T Get(object id)
        {
            return _table.Find(id);
        }

        public List<T> GetAll()
        {
            return _table.ToList();
        }

        public ErrorCode Update(object id, T t, out string errorMsg)
        {
            try
            {
                var old_obj = Get(id);
                if (old_obj == null)
                {
                    errorMsg = "Entity not found";
                    return ErrorCode.Error;
                }

                _db.Entry(old_obj).CurrentValues.SetValues(t);
                _db.SaveChanges();
                errorMsg = "Updated";
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode Save(out string errorMsg)
        {
            errorMsg = string.Empty;
            try
            {
                _db.SaveChanges();
                errorMsg = "Saved successfully.";
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return ErrorCode.Error;
            }
        }

    }
}