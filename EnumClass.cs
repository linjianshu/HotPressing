using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperClass
{
    public class EnumClass
    {
        public enum Submit : int
        {
            Submit = 1,//报工
            NotSubmit=0//不保工
        }
        public enum EquipmentState : int
        {
            Load= 1,//负载
            Idling=2,//空转
        }
        public enum ProcedureType : int
        {
            Machining = 1,//机加工
            Fitter = 2,//钳工
            Outsourcing=3,//外协
            HotPressing =4,//热压工序
            Tooling = 5,//工装工序
        }
        public enum ApprovalStatus : int
        {
            PendingReview = 1,//待审核
            Audited = 2,//已审核
        }
    }
}
