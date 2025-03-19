using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Database;
using Dealogikal.Utils;

namespace Dealogikal.Repository
{
    public class ImageManager
    {
        BaseRepository<images> _img;


        public ImageManager()
        {
            _img = new BaseRepository<images>();
        }

        public List<images> ListImageByEmployeeId(string employeeId)
        {
            return _img._table.Where(m => m.employeeId == employeeId).ToList();
        }

        public images GetImagebyEmployeeId(string employeeId)
        {
            return _img._table.FirstOrDefault(i => i.employeeId == employeeId);
        }

        public List<images> GetAllImages()
        {
            return _img.GetAll();
        }

        public ErrorCode CreateImg(images img, ref string err)
        {
            return _img.Create(img, out err);
        }
        public ErrorCode UpdateImg(images img, ref String err)
        {
            return _img.Update(img.mediaId, img, out err);
        }
        public ErrorCode DeleteImg(int? id, ref String err)
        {
            return _img.Delete(id, out err);
        }
    }


}