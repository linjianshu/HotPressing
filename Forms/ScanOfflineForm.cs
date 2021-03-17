using AutoMapper;
using HZH_Controls;
using HZH_Controls.Forms;
using QualityCheckDemo;
using System;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace MachineryProcessingDemo
{
    public partial class ScanOfflineForm : FrmWithOKCancel1
    {
        public ScanOfflineForm(string productBornCode, long? staffId, string staffCode, string staffName)
        {
            _productBornCode = productBornCode;
            _staffId = staffId;
            _staffCode = staffCode;
            _staffName = staffName;
            InitializeComponent();
        }
        private static string _workshopId;
        private static string _workshopCode;
        private static string _workshopName;
        private static string _equipmentId;
        private static string _equipmentCode;
        private static string _equipmentName;
        private static long? _staffId;
        private static string _staffCode;
        private static string _staffName;
        private static string _productBornCode;
        private static C_ProductProcessing _cProductProcessing;
        public Action ChangeBgColor;
        public Action ClearMainPanelTxt;
        public Action RegetProcedureTasksDetails;
        private void ScanOfflineForm_Load(object sender, EventArgs e)
        {
            var addXmlFile = new ConfigurationBuilder().SetBasePath("E:\\project\\visual Studio Project\\HotPressing")
                .AddXmlFile("config.xml");
            var configuration = addXmlFile.Build();
            _workshopId = configuration["WorkshopID"];
            _workshopCode = configuration["WorkshopCode"];
            _workshopName = configuration["WorkshopName"];
            _equipmentId = configuration["EquipmentID"];
            _equipmentCode = configuration["EquipmentCode"];
            _equipmentName = configuration["EquipmentName"];

            DataFill();
            Initialize();
        }

        private void Initialize()
        {
            using (var context = new Model())
            {
                _cProductProcessing = context.C_ProductProcessing.FirstOrDefault(s => s.ProductBornCode==_productBornCode);
            }
        }

        private void DataFill()
        {
            var tuple = new Tuple<string, string>("扫码下线", "A_fa_cube");
            FontIcons icon1 = (FontIcons)Enum.Parse(typeof(FontIcons), tuple.Item2);
            var pictureBox1 = new PictureBox
            {
                AutoSize = false,
                Size = new Size(40, 40),
                ForeColor = Color.FromArgb(255, 77, 59),
                Image = FontImages.GetImage(icon1, 40, Color.FromArgb(255, 77, 59)),
                Location = new Point(this.Size.Width / 2 - 20, 15)
            };
            panel3.Controls.Add(pictureBox1);

            using (var context = new Model())
            {
                var cProductProcessing = context.C_ProductProcessing.First(s => s.ProductBornCode == _productBornCode);
                BeginInvoke(new Action(() =>
                {
                    ProductIDTxt.Text = _productBornCode;
                    ProductIDTxt.ReadOnly = true;
                    ProductNameTxt.Text = cProductProcessing.ProductName;
                    ProductNameTxt.ReadOnly = true;
                }));
            }
        }
        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text == "不良下机")
            {
                BadReasonLbl.Visible = true;
                richTextBox1.Visible = true;
            }
            else
            {
                BadReasonLbl.Visible = false;
                richTextBox1.Visible = false;
            }
        }
        protected override void DoEnter()
        {
            {
                if (IsLastProcedureEeceptQC())
                    {
                        //如果是除了质检外的末道工序 生成产品档案表 师兄,末道工序转到产品档案表里是指的是机加工序的末道工序还是整个产品而言的末道工序???
                        GenerateProductDoc();
                    }
                
                    //如果是不良下机则生成过程检验任务
                    if (comboBox1.Text.Trim().Equals("不良下机"))
                    {
                        GenerateProcessTask((int) CheckReason.Bad);
                        RemarkInspect();
                        FrmDialog.ShowDialog(this, "请将此 产品送至质检中心,等待进一步检验结果!");
                        //首件记录转档
                        // FirstTurnDoc();
                    }
                    //如果是返修上线则生成过程检验任务
                    else if (_cProductProcessing.Online_type == (decimal?) ProductProcessingOnlineType.Repair)
                    {
                        //还没测试
                        GenerateProcessTask((int) CheckReason.Repair);
                        RemarkInspect();
                        FrmDialog.ShowDialog(this, "请将此 产品送至质检中心,等待进一步检验结果!");
                    }
                
                //apsdetail状态修改以及完善apsdetail最后修改时间呀什么的
                PerfectApsDetail();

                //控制点转档
                OfflineCntLogicTurn();
                //计划产品信息状态修改(不应该出现,应该出现在某道产品末道工序的最后一个产品上 可以用goto语句搞一搞)
                // PlanProductInfoStateChange();
                //完善产品质量数据表
                // PerfectProductQD();

                //修改aps工序任务表里的信息,例如修改完成状态,更新执行进度
                PerfectApsProcedureTask();

                //产品加工过程表转档
                ProductProcessingDocTurn();
                FrmDialog.ShowDialog(this, "产品下线成功", "提示");
                ChangeBgColor();
                RegetProcedureTasksDetails();
            }
            Close();
        }
        private void GenerateProductDoc()
        {
            using (var context = new Model())
            {
                var mapperConfiguration = new MapperConfiguration(cfg =>
                    cfg.CreateMap<C_ProductProcessing, C_ProductDocument>());
                var mapper = mapperConfiguration.CreateMapper();
                var cProcedureFirstDocument = mapper.Map<C_ProductDocument>(_cProductProcessing);
                //需要修改
                cProcedureFirstDocument.Type = -1;
                cProcedureFirstDocument.OfflineTime = context.GetServerDate();
                cProcedureFirstDocument.IsAvailable = true;
                cProcedureFirstDocument.CreateTime = context.GetServerDate();
                cProcedureFirstDocument.CreatorID = _staffId;
                cProcedureFirstDocument.LastModifiedTime = context.GetServerDate();
                cProcedureFirstDocument.ModifierID = _staffId;

                var cProductProcessingDocument = context.C_ProductProcessingDocument.Where(s => s.ProductBornCode == _productBornCode
                         && s.PlanCode == _cProductProcessing.PlanCode
                        && s.PlanID == _cProductProcessing.PlanID)
                    .OrderBy(s => s.OfflineTime).FirstOrDefault();
                cProcedureFirstDocument.OnlineTime = cProductProcessingDocument.OnlineTime;
                context.C_ProductDocument.Add(cProcedureFirstDocument);
                context.SaveChanges();
            }
        }
        private bool IsLastProcedureEeceptQC()
        {
            using (var context = new Model())
            {
                //在产品工序基础表里 根据项目号/计划号/产品号/有效性/工序类型(去掉)  对工序索引进行排序 获得末道工序信息
                var aProductProcedureBase = context.A_ProductProcedureBase.Where(s =>
                        s.ProjectID == _cProductProcessing.ProjectID && s.PlanID == _cProductProcessing.PlanID
                                                                     && s.ProductID == _cProductProcessing.ProductID
                                                                     && s.IsAvailable == true)
                    .OrderByDescending(s => s.ProcedureIndex).First();
                //判断末道工序是不是当前工序
                if (aProductProcedureBase.ProcedureID.ToString() == _cProductProcessing.ProcedureID)
                {
                    return true;
                }
                    return false;
            }
        }
        private void PerfectApsProcedureTask()
        {
            using (var context = new Model())
            {
                //在工序任务表里 根据订单号/计划号/项目号/产品号/工序号/设备号获得元数据
                var apsProcedureTask = context.APS_ProcedureTask.First(s =>
                    s.OrderID == _cProductProcessing.OrderID && s.PlanCode == _cProductProcessing.PlanCode &&
                    s.ProjectCode == _cProductProcessing.ProjectCode &&
                    s.ProductCode == _cProductProcessing.ProductCode &&
                    s.ProcedureCode == _cProductProcessing.ProcedureCode &&
                    s.EquipmentID == _cProductProcessing.EquipmentID);

                var count = context.APS_ProcedureTaskDetail.Count(s =>
                    s.EquipmentID == _cProductProcessing.EquipmentID && s.TaskTableID == apsProcedureTask.ID &&
                    s.TaskState == (decimal?) ApsProcedureTaskDetailState.Completed && s.IsAvailable == true);

                decimal progressPercent = (decimal)((double)count / apsProcedureTask.ProductNumber);

                apsProcedureTask.ProgressPercent = progressPercent;
                apsProcedureTask.ModifierID = _staffId.ToString();
                apsProcedureTask.LastModifiedTime = context.GetServerDate();

                if (progressPercent == 1)
                {
                    apsProcedureTask.EndTime = context.GetServerDate();
                    apsProcedureTask.TaskState = (int?) ApsProcedureTaskState.Completed; //已完成呜呜呜
                }

                context.SaveChanges();
            }
        }
        private void OfflineCntLogicTurn()
        {
            using (var context = new Model())
            {
                //在产品加工过程表中根据产品出生证  获取元数据
                _cProductProcessing = context.C_ProductProcessing.FirstOrDefault(s => s.ProductBornCode == ProductIDTxt.Text.Trim());
                //在控制点过程表中 根据产品出生证 工序编号 控制点id 设备编号(需要修改) 查到相关集合
                var cBWuECntlLogicPros = context.C_BWuE_CntlLogicPro.Where(s =>
                        s.ProductBornCode == ProductIDTxt.Text.Trim() && s.ProcedureCode == _cProductProcessing.ProcedureCode
                                                                      && s.ControlPointID == 8 && s.EquipmentCode == _equipmentCode)
                    .OrderByDescending(s => s.StartTime).ToList();
                //判断过程表中有无数据  如果没有(说明已经转档过了) 那就不操作了弟弟
                if (cBWuECntlLogicPros.Any())
                {
                    cBWuECntlLogicPros[0].State = "2";
                    cBWuECntlLogicPros[0].FinishTime = context.GetServerDate();

                    //遍历  添加后删除过程表中所有选中数据
                    foreach (var cBWuECntlLogicPro in cBWuECntlLogicPros)
                    {
                        var mapperConfiguration = new MapperConfiguration(cfg =>
                            cfg.CreateMap<C_BWuE_CntlLogicPro, C_BWuE_CntlLogicDoc>());
                        var mapper = mapperConfiguration.CreateMapper();
                        var cBWuECntlLogicDoc = mapper.Map<C_BWuE_CntlLogicDoc>(cBWuECntlLogicPro);

                        context.C_BWuE_CntlLogicDoc.Add(cBWuECntlLogicDoc);
                        context.C_BWuE_CntlLogicPro.Remove(cBWuECntlLogicPro);
                    }
                    context.SaveChanges();
                }
            }
        }
        private void PerfectApsDetail()
        {
            using (var context = new Model())
            {
                var apsProcedureTaskDetail = context.APS_ProcedureTaskDetail.First(s =>
                    s.EquipmentID == _cProductProcessing.EquipmentID &&
                    s.ProductBornCode == _cProductProcessing.ProductBornCode &&
                    s.ProcedureCode == _cProductProcessing.ProcedureCode && s.IsAvailable == true &&
                    s.TaskState == (decimal?) ApsProcedureTaskDetailState.InExcecution);
                apsProcedureTaskDetail.TaskState = (int?) ApsProcedureTaskDetailState.Completed;//已完成
                apsProcedureTaskDetail.ModifierID = _staffId.ToString();
                apsProcedureTaskDetail.LastModifiedTime = context.GetServerDate();
                context.Entry(apsProcedureTaskDetail).State = EntityState.Modified;
                context.SaveChanges();
            }
        }
        private void ProductProcessingDocTurn()
        {
            using (var context = new Model())
            {
                _cProductProcessing.OfflineStaffID = _staffId;
                _cProductProcessing.OfflineStaffCode = _staffCode;
                _cProductProcessing.OfflineStaffName = _staffName;

                if (comboBox1.Text.Trim().Contains("正常"))
                {
                    _cProductProcessing.Offline_type = (int?) ProductProcessingOfflineType.Normal;
                }
                else
                {
                    _cProductProcessing.Offline_type = (int?) ProductProcessingOfflineType.Bad;//不良
                    _cProductProcessing.CauseDescription = richTextBox1.Text.Trim();
                }
                _cProductProcessing.OfflineTime = context.GetServerDate();
                context.Entry(_cProductProcessing).State = EntityState.Deleted;

                var mapperConfiguration = new MapperConfiguration(cfg =>
                    cfg.CreateMap<C_ProductProcessing, C_ProductProcessingDocument>());
                var mapper = mapperConfiguration.CreateMapper();
                var cProductProcessingDocument = mapper.Map<C_ProductProcessingDocument>(_cProductProcessing);
                context.C_ProductProcessingDocument.Add(cProductProcessingDocument);

                context.SaveChanges();
            }
        }
        private void RemarkInspect()
        {
            using (var context = new Model())
            {
                var apsProcedureTaskDetail = context.APS_ProcedureTaskDetail.First(s =>
                    s.ProductBornCode == _cProductProcessing.ProductBornCode &&
                    s.ProcedureCode == _cProductProcessing.ProcedureCode && s.IsAvailable == true
                    && s.TaskState == (decimal?) ApsProcedureTaskDetailState.InExcecution);
                apsProcedureTaskDetail.IsInspect = 1;
                context.SaveChanges();
            }
        }
        private void GenerateProcessTask(int checkReason, int checktype = (int)CheckType.ThreeCoordinate)
        {
            using (var context = new Model())
            {
                var mapperConfiguration = new MapperConfiguration(cfg =>
                    cfg.CreateMap<C_ProductProcessing, C_CheckTask>());
                var mapper = mapperConfiguration.CreateMapper();
                var cCheckTask = mapper.Map<C_CheckTask>(_cProductProcessing);

                cCheckTask.ProcedureID = _cProductProcessing.ProcedureID;
                cCheckTask.TaskState = (int?) CheckTaskState.NotOnline;
                //原因
                cCheckTask.CheckReason = checkReason;//最后一道工序
                cCheckTask.CheckType = checktype;//3 手检
                cCheckTask.IsAvailable = true;
                cCheckTask.CreateTime = context.GetServerDate();
                cCheckTask.CreatorID = _staffId;
                cCheckTask.LastModifiedTime = context.GetServerDate();
                cCheckTask.ModifierID = _staffId;
                
                //需要修改
                cCheckTask.WorkerCode = DistributeCheckTask().StaffCode + ',';

                context.C_CheckTask.Add(cCheckTask);
                context.SaveChanges();
            }
        }

        private C_StaffBaseInformation DistributeCheckTask(string SkillType = "三坐标")
        {
            using (var context = new Model())
            {
                //在人员基础信息表里查找三坐标和质检两类人员  这里生成的是三坐标任务 所以检索三坐标人员
                var staffBaseInformation = context.C_StaffBaseInformation
                    .Where(s => s.SkillType == SkillType && s.IsAvailable == true).OrderBy(s => s.Reserve1).FirstOrDefault();
                if (staffBaseInformation.Reserve1 == null)
                {
                    staffBaseInformation.Reserve1 = 0;
                }
                staffBaseInformation.Reserve1 = staffBaseInformation.Reserve1 + 1;
                context.SaveChanges();
                return staffBaseInformation;
            }
        }
    }
}
