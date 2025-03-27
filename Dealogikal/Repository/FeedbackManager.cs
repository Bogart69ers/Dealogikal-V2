using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Database;
using Dealogikal.Utils;

namespace Dealogikal.Repository
{
    public class FeedbackManager
    {

        private BaseRepository<feedback> _feedback;


        public FeedbackManager()
        {
            _feedback = new BaseRepository<feedback>();
        }

        public List<feedback> GetAllFeedbackDesc()
        {
            return _feedback.GetAll()
                            .OrderBy(l => l.status != 0) 
                            .ThenByDescending(l => l.dateCreated) 
                            .ToList();
        }

        public List<feedback> GetAllFeedback()
        {
            return _feedback.GetAll();
        }

        public feedback GetFeedbackById(int id)
        {
            return _feedback.Get(id);
        }

        public ErrorCode CreateFeedback(feedback fb, ref string errMsg)
        {
            try
            {

                if (_feedback.Create(fb, out errMsg) != ErrorCode.Success)
                {
                    return ErrorCode.Error;
                }

                return ErrorCode.Success;

            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode UpdateFeedbackStatus(int id, ref string errMsg)
        {
            try
            {
                var feedback = GetFeedbackById(id);
                if (feedback == null)
                {
                    errMsg = "No Feedback Information found.";
                    return ErrorCode.Error;
                }

                feedback.status = 1;

                return _feedback.Update(id, feedback, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }
    }
}