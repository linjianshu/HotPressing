using AutoMapper;
using HZH_Controls;
using HZH_Controls.Forms;
using QualityCheckDemo;
using System;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using WorkPlatForm.Public_Classes;

namespace MachineryProcessingDemo
{
    public partial class ScanOnlineForm : FrmWithOKCancel1
    {
        public ScanOnlineForm(long? staffId, string staffCode, string staffName)
        {
            _staffId = staffId;
            _staffCode = staffCode;
            _staffName = staffName;
            InitializeComponent();
        }
        public ScanOnlineForm(long? staffId, string staffCode, string staffName, string productBornCode)
        {
            _staffId = staffId;
            _staffCode = staffCode;
            _staffName = staffName;
            _productBornCode = productBornCode;
            InitializeComponent();
            ProductIDTxt.Text = _productBornCode;
        }
        public ScanOnlineForm(long? staffId, string staffCode, string staffName, string productBornCode, string workshopId, string workshopCode, string workshopName,
            string equipmentId, string equipmentCode, string equipmentName)
        {
            _staffId = staffId;
            _staffCode = staffCode;
            _staffName = staffName;
            _productBornCode = productBornCode;
            InitializeComponent();
            ProductIDTxt.Text = _productBornCode;
            _workshopId = workshopId;
            _workshopCode = workshopCode;
            _workshopName = workshopName;
            _equipmentId = equipmentId;
            _equipmentCode = equipmentCode;
            _equipmentName = equipmentName;
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
        private static string _strProductType = "产品";
        private static APS_ProcedureTask _currentTask;
        public Action RegetProcedureTasksDetails;
        public Action<string, string, string, string> DisplayInfoToMainPanel;
        public Action ChangeBgColor;
        public Action ClearMainPanelTxt;
        private void ScanOnline_Load(object sender, EventArgs e)
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

            var tuple = new Tuple<string, string>("扫码上线", "A_fa_cube");
            var icon1 = (FontIcons)Enum.Parse(typeof(FontIcons), tuple.Item2);
            var pictureBox1 = new PictureBox
            {
                AutoSize = false,
                Size = new Size(40, 40),
                ForeColor = Color.FromArgb(255, 77, 59),
                Image = FontImages.GetImage(icon1, 40, Color.FromArgb(255, 77, 59)),
                Location = new Point(this.Size.Width / 2 - 20, 30)
            };
            panel3.Controls.Add(pictureBox1);

            if (serialPortTest.IsOpen) { serialPortTest.Close(); }
            string portName = ConfigAppSettingsHelper.ReadSetting("PortName");
            string baudRate = ConfigAppSettingsHelper.ReadSetting("BaudRate");
            serialPortTest.Dispose();//释放扫描枪所有资源
            serialPortTest.PortName = portName;
            serialPortTest.BaudRate = int.Parse(baudRate);
            try
            {
                if (!serialPortTest.IsOpen)
                {
                    serialPortTest.Open();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }
        /// <summary>
        /// 扫描枪扫描调用方法
        /// </summary>
        /// <param name="serialPort"></param>
        /// <returns></returns>
        private static string GetDataFromSerialPort(SerialPort serialPort)
        {
            Thread.Sleep(300);
            byte[] buffer = new byte[serialPort.BytesToRead];
            string receiveString = "";
            try
            {
                serialPort.Read(buffer, 0, buffer.Length);
                foreach (var t in buffer)
                {
                    receiveString += (char)t;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            if (receiveString.Length > 2)
            {
                receiveString = receiveString.Substring(0, receiveString.Length - 1);
            }
            return receiveString;
        }

        //清空窗体textbox 解开只读限制
        private void ClearTxt()
        {
            BeginInvoke(new Action(() =>
            {
                ProductIDTxt.Clear();
                ProductIDTxt.ReadOnly = false;
                ProductNameTxt.Clear();
                ProductNameTxt.ReadOnly = false;
                _productBornCode = "";
            }));
        }
        private void serialPortTest_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {
            ClearTxt();
            var receivedData = GetDataFromSerialPort(serialPortTest);

            if (CheckProductBornCode(receivedData))
            {
                CheckTaskValidity();
            }
        }
        private bool CheckProductBornCode(string receivedData)
        {
            using (var context = new Model())
            {
                BeginInvoke(new Action(() =>
                {
                    ProductIDTxt.Text = receivedData;
                    ProductIDTxt.ReadOnly = true;
                }));

                // if (_strProductType.Contains("产品"))
                {
                    //在计划产品出生证表中根据产品出生证来获取产品名称(要添加一个状态)
                    var aPlanProductInflammations = context.A_PlanProductInfomation.FirstOrDefault(s =>
                        s.ProductBornCode == receivedData && s.IsAvailable == true);

                    if (aPlanProductInflammations != null)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            ProductNameTxt.Text = aPlanProductInflammations.ProductName;
                            ProductNameTxt.ReadOnly = true;
                        }));
                        _productBornCode = receivedData;
                        return true;
                    }
                    else
                    {
                        BeginInvoke(new Action((() =>
                            FrmDialog.ShowDialog(this, "产品出生证不正确", "出生证不正确"))));
                        // MessageBox.Show("产品出生证不正确");
                        ClearTxt();
                        _productBornCode = "";
                        return false;
                    }
                }
            }
        }
        public bool CheckTaskValidity()
        {
            using (var context = new Model())
            {
                {
                    //在工序任务明细表中根据产品出生证和设备编号以及任务完成状态  判断有无加工任务
                    var apsProcedureTaskDetail = context.APS_ProcedureTaskDetail.First(s =>
                        s.ProductBornCode == _productBornCode && s.EquipmentID == 4 && s.TaskState == (decimal?) ApsProcedureTaskDetailState.NotOnline);
                    if (apsProcedureTaskDetail == null)
                    {
                        BeginInvoke(new Action((() =>
                            FrmDialog.ShowDialog(this, "该加工中心暂无当前产品加工任务", "暂无加工任务"))));
                        ClearTxt();
                        return false;
                    }

                    //在工序任务表中根据设备编号和任务状态和 开始时间排序得到优先级最高的工序任务
                    // 待完成的工序编号(弃用)
                    var apsProcedureTasks = context.APS_ProcedureTask.Where(s =>
                            s.EquipmentID == 4 && s.TaskState == (decimal?) ApsProcedureTaskState.ToDo)
                        .OrderBy(s => s.StartTime).FirstOrDefault();
                    // &&s.ProcedureCode == apsProcedureTaskDetail.ProcedureCode


                    if (apsProcedureTasks != null)
                    {
                        _currentTask = apsProcedureTasks;
                        //判断是否已经加工过了
                        var procedureTaskDetail = context.APS_ProcedureTaskDetail.First(s =>
                            s.ProductBornCode == ProductIDTxt.Text.Trim() &&
                            s.ProcedureCode == apsProcedureTasks.ProcedureCode);

                        if (procedureTaskDetail.TaskState == (decimal?) ApsProcedureTaskDetailState.Completed)
                        {
                            BeginInvoke(new Action((() =>
                                FrmDialog.ShowDialog(this, "该工序任务已完成", "提示"))));
                            ClearTxt();
                            return false;
                        }

                        //在工序任务明细表中 根据 任务表id==明细表工序任务id 获得优先级最高的工序任务下的产品出生证集合
                        var list = context.APS_ProcedureTaskDetail.Where(s => s.TaskTableID == apsProcedureTasks.ID)
                            .Select(s => s.ProductBornCode).ToList();

                        //判断扫码的产品出生证是否在该集合里
                        if (!list.Contains(_productBornCode))
                        {
                            BeginInvoke(new Action((() =>
                                FrmDialog.ShowDialog(this, "该任务不是推荐生产顺序,请重新扫码", "生产顺序异常"))));
                            ClearTxt();
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        BeginInvoke(new Action((() =>
                            FrmDialog.ShowDialog(this, "该加工中心暂无当前产品加工任务", "暂无加工任务"))));
                        ClearTxt();
                        return false;
                    }
                }
            }
        }
        public bool CheckTaskValidity(string procedureCode)
        {
            using (var context = new Model())
            {
                {
                    //在工序任务明细表中根据产品出生证和设备编号以及任务完成状态  判断有无加工任务
                    var apsProcedureTaskDetail = context.APS_ProcedureTaskDetail.First(s =>
                        s.ProductBornCode == _productBornCode && s.EquipmentID == 4 && s.TaskState == (decimal?) ApsProcedureTaskDetailState.NotOnline);
                    if (apsProcedureTaskDetail == null)
                    {
                        BeginInvoke(new Action((() =>
                            FrmDialog.ShowDialog(this, "该加工中心暂无当前产品加工任务", "暂无加工任务"))));
                        ClearTxt();
                        return false;
                    }

                    //在工序任务表中根据设备编号和任务状态和 开始时间排序得到优先级最高的工序任务
                    // 待完成的工序编号(弃用)
                    var apsProcedureTasks = context.APS_ProcedureTask.Where(s =>
                            s.EquipmentID == 4 && s.TaskState == (decimal?) ApsProcedureTaskState.ToDo)
                        .OrderBy(s => s.StartTime).FirstOrDefault();
                    // &&s.ProcedureCode == apsProcedureTaskDetail.ProcedureCode

                    //判断工序号对不对应,  防止出现同一个产品但是先加工了op02的情况    
                    if (apsProcedureTasks.ProcedureCode != procedureCode)
                    {
                        FrmDialog.ShowDialog(this, "该任务不是推荐生产工序顺序,请重新扫码", "生产工序顺序异常");
                        return false;
                    }

                    if (apsProcedureTasks != null)
                    {
                        _currentTask = apsProcedureTasks;
                        //判断是否已经加工过了
                        var procedureTaskDetail = context.APS_ProcedureTaskDetail.First(s =>
                            s.ProductBornCode == ProductIDTxt.Text.Trim() &&
                            s.ProcedureCode == apsProcedureTasks.ProcedureCode);

                        if (procedureTaskDetail.TaskState == (decimal?) ApsProcedureTaskDetailState.Completed)
                        {
                            BeginInvoke(new Action((() =>
                                FrmDialog.ShowDialog(this, "该工序任务已完成", "提示"))));
                            ClearTxt();
                            return false;
                        }

                        //在工序任务明细表中 根据 任务表id==明细表工序任务id 获得优先级最高的工序任务下的产品出生证集合
                        var list = context.APS_ProcedureTaskDetail.Where(s => s.TaskTableID == apsProcedureTasks.ID)
                            .Select(s => s.ProductBornCode).ToList();

                        //判断扫码的产品出生证是否在该集合里
                        if (!list.Contains(_productBornCode))
                        {
                            FrmDialog.ShowDialog(this, "该任务不是推荐生产顺序,请重新扫码", "生产顺序异常");
                            ClearTxt();
                            return false;
                        }

                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        BeginInvoke(new Action((() =>
                            FrmDialog.ShowDialog(this, "该加工中心暂无当前产品加工任务", "暂无加工任务"))));
                        // FrmDialog.ShowDialog(this, "该加工中心暂无当前产品加工任务", "暂无加工任务");
                        // MessageBox.Show("该加工中心暂无当前产品加工任务");
                        ClearTxt();
                        return false;
                    }
                }
            }
        }
        protected override void DoEnter()
        {
            if (CheckProductBornCode(ProductIDTxt.Text.Trim()))
            {
                if (CheckTaskValidity())
                {
                    //判断机加工任务是否全部完成
                    var doneMachiningOrNot = DoneMachiningOrNot(ProductIDTxt.Text.Trim());
                    if (!doneMachiningOrNot)
                    {
                        FrmDialog.ShowDialog(this, "尚有机加工环节未执行,请先执行机加工任务!");
                        return;
                    }
                    AddCntLogicPro();
                    //先判断一下本产品出生证的有没有待检验的前序质检任务没做
                    // var hasSelfQcTask = HasSelfQcTask();
                    
                            //转档  工序任务明细表=>产品加工过程表
                            ProcessTurnArchives();
                            //完善工序任务明细表中的数据 诸如任务状态 ; 修改人修改时间
                            PerfectApsDetail();
                            //完善计划产品出生证表
                            // PerfectPlanProductInfo();
                            //转档  
                            CntLogicTurn();
                            FrmDialog.ShowDialog(this, $"产品{ProductIDTxt.Text}热压上线成功!", "热压上线成功");
                            //刷新界面数据
                            RegetProcedureTasksDetails();
                }
            }
        }
        private bool DoneMachiningOrNot(string bornCode)
        {
            using (var context = new Model())
            {
                var apsProcedureTaskDetails = context.APS_ProcedureTaskDetail.Where(s =>
                    s.ProcedureType == (decimal?)ProcedureType.Machining &&
                    s.ProductBornCode == bornCode).ToList();
                if (apsProcedureTaskDetails.All(s => s.TaskState == (decimal?)ApsProcedureTaskDetailState.Completed))
                {
                    return true;
                }
                return false;
            }
        }
        public void PerfectApsDetail()
        {
            using (var context = new Model())
            {
                var cProductProcessing = context.C_ProductProcessing.First(s => s.ProductBornCode == _productBornCode);
                var apsProcedureTaskDetail = context.APS_ProcedureTaskDetail.First(s =>
                    s.EquipmentID == cProductProcessing.EquipmentID &&
                    s.ProductBornCode == cProductProcessing.ProductBornCode &&
                    s.ProcedureCode == cProductProcessing.ProcedureCode && s.IsAvailable == true &&
                    s.TaskState == (decimal?) ApsProcedureTaskDetailState.NotOnline);
                apsProcedureTaskDetail.TaskState = 2; //正在进行中
                apsProcedureTaskDetail.LastModifiedTime = context.GetServerDate();
                apsProcedureTaskDetail.ModifierID = _staffId.ToString();
                context.SaveChanges();
            }
        }
        public void CntLogicTurn()
        {
            using (var context = new Model())
            {
                //在控制点过程表中 根据产品出生证 工序编号 控制点id 设备编号(需要修改) 查到相关集合
                var cBWuECntlLogicPros = context.C_BWuE_CntlLogicPro.Where(s =>
                        s.ProductBornCode == _productBornCode && s.ProcedureCode == _currentTask.ProcedureCode
                                                              && s.ControlPointID == 7 && s.EquipmentCode == "4")
                    .OrderByDescending(s => s.StartTime).ToList();
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
        public void AddCntLogicPro()
        {
            //录入进控制点过程表  哪个产品在哪个工序哪个控制点正在进行
            using (var context = new Model())
            {
                var cBWuECntlLogicPro = new C_BWuE_CntlLogicPro();
                cBWuECntlLogicPro.ProductBornCode = _productBornCode;
                cBWuECntlLogicPro.ProcedureCode = _currentTask.ProcedureCode;
                cBWuECntlLogicPro.ControlPointID = 7;
                cBWuECntlLogicPro.Sort = 1.ToString();
                cBWuECntlLogicPro.EquipmentCode = "4";
                cBWuECntlLogicPro.State = 1.ToString();
                cBWuECntlLogicPro.StartTime = context.GetServerDate();

                context.C_BWuE_CntlLogicPro.Add(cBWuECntlLogicPro);
                context.SaveChanges();
            }
        }
        //操作成员确认
        public bool WorkerConfirm()
        {
            using (var context = new Model())
            {
                var strings = _currentTask.WorkerCode.Split(',');
                var contains = strings.Contains(_staffCode);
                if (!contains)
                {
                    var dialogResult = (DialogResult)Invoke(new Func<DialogResult>((() =>
                        FrmDialog.ShowDialog(this, "加工人员与规定人员不一致,是否继续生产",
                            "人员不匹配", true))));
                    // var dialogResult = MessageBox.Show("加工人员与规定人员不一致,是否继续生产", "人员不匹配", MessageBoxButtons.OKCancel);
                    if (dialogResult == DialogResult.OK)
                    {
                        if (!IsHighLevel())
                        {
                            var cInformationPushProcessing = new C_InfomationPushProcessing
                            {
                                //这里需要改动
                                // PushID = "push001",
                                PushCategory = "等级异常",
                                InitPushRankPushRank = "1",
                                PushContent = "热压环节人员操作等级未达要求",
                                CreateType = "现场发起",
                                PushState = 1,
                                CreateTime = context.GetServerDate(),
                                CreatorID = _staffId
                            };

                            context.C_InfomationPushProcessing.Add(cInformationPushProcessing);
                            context.SaveChanges();

                            BeginInvoke(new Action((() =>
                                FrmDialog.ShowDialog(this, "很抱歉,您的操作等级未能达到要求,已将消息推送至主管", "等级异常"))));
                            ClearTxt();
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        ClearTxt();
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
        }
        private bool IsHighLevel()
        {
            bool b = false;
            using (var context = new Model())
            {
                var cStaffBaseInformation = context.C_StaffBaseInformation.FirstOrDefault(s =>
                    s.StaffCode == _staffCode && s.StaffName == _staffName && s.IsAvailable == true);
                int.TryParse(cStaffBaseInformation.SkillGrade, out var result);
                var strings = _currentTask.WorkerCode.Split(',');
                foreach (var s1 in strings)
                {
                    var staffBaseInformation = context.C_StaffBaseInformation.FirstOrDefault(s => s.StaffCode == s1 && s.IsAvailable == true);
                    if (staffBaseInformation != null)
                    {
                        int.TryParse(staffBaseInformation.SkillGrade, out var result1);
                        if (result < result1)
                        {
                            return false;
                        }
                        if (result >= result1)
                        {
                            b = true;
                        }
                    }
                }
            }
            //这里需要改动
            return b;
        }
        public bool KittingConfirm()
        {
            using (var context = new Model())
            {
                //先从工序基础表中 根据工序编号判断是否有齐套要求  ????当前产品生产绑定在特定的计划下 是否只需要查找该计划是否齐套就ok了????
                var aProcedureBase = context.A_ProcedureBase.FirstOrDefault(s => s.ProcedureCode == _currentTask.ProcedureCode);

                var aMaterialProgramDemand = context.A_MaterialProgramDemand.Where(s =>
                    s.ProjectID == _currentTask.ProjectID && s.PlanCode == _currentTask.PlanCode).ToList();

                var materialProgramAll = aMaterialProgramDemand.All(s => s.Cstate == 1);

                var aCutterDemands = context.A_CutterDemand.Where(s =>
                    s.ProjectCode == _currentTask.ProjectCode && s.PlanCode == _currentTask.PlanCode).ToList();

                var cutterAll = aCutterDemands.All(s => s.Cstate == 1);

                if (!materialProgramAll || !cutterAll)
                {
                    DialogResult dialogResult = DialogResult.None;
                    if (!materialProgramAll && !cutterAll)
                    {
                        if (!this.IsHandleCreated)
                        {
                            dialogResult = FrmDialog.ShowDialog(this, "物料程序和刀具均未齐套,是否继续生产?", "未齐套", true);
                        }
                        else
                        {
                            dialogResult = (DialogResult)Invoke(new Func<DialogResult>((() =>
                            FrmDialog.ShowDialog(this, "物料程序和刀具均未齐套,是否继续生产?", "未齐套", true))));
                        }
                    }
                    else if (!materialProgramAll)
                    {
                        if (!this.IsHandleCreated)
                        {
                            dialogResult = FrmDialog.ShowDialog(this, "物料或程序未齐套,是否继续生产?", "未齐套", true);
                        }
                        else
                        {
                            dialogResult = (DialogResult)Invoke(new Func<DialogResult>((() =>
                                FrmDialog.ShowDialog(this, "物料或程序未齐套,是否继续生产?", "未齐套", true))));
                        }
                    }
                    else
                    {
                        if (!this.IsHandleCreated)
                        {
                            dialogResult = FrmDialog.ShowDialog(this, "刀具未齐套,是否继续生产?", "未齐套", true);
                        }
                        else
                        {
                            dialogResult = (DialogResult)Invoke(new Func<DialogResult>((() =>
                            FrmDialog.ShowDialog(this, "刀具未齐套,是否继续生产?", "未齐套", true))));
                        }
                    }

                    if (dialogResult == DialogResult.OK)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public void ProcessTurnArchives()
        {
            var mapperConfiguration = new MapperConfiguration(cfg =>
                cfg.CreateMap<APS_ProcedureTask, C_ProductProcessing>());
            var mapper = mapperConfiguration.CreateMapper();
            var cProductProcessing = mapper.Map<C_ProductProcessing>(_currentTask);
            cProductProcessing.WorkshopID = 4;
            cProductProcessing.WorkshopCode = "004";
            cProductProcessing.WorkshopName = "热压中心001";

           cProductProcessing.EquipmentCode = "004";
           cProductProcessing.EquipmentName = "热压机";
            cProductProcessing.EquipmentID = 4;

            using (var context = new Model())
            {
                //在计划产品出生证表中 根据计划编号/有效性/产品出生证   获得计划id 并赋值
                var aPlanProductInformation = context.A_PlanProductInfomation.FirstOrDefault(s =>
                    s.PlanCode == _currentTask.PlanCode && s.IsAvailable == true && s.ProductBornCode == _productBornCode);
                if (aPlanProductInformation != null) cProductProcessing.PlanID = aPlanProductInformation.PlanID;

                cProductProcessing.ProductBornCode = _productBornCode;

                //在产品工序基本表中根据工序编号/有效性/产品号/计划号          获得工序id和名称   这里是否需要外加计划id项目id和产品id才能查得到????
                var aProductProcedureBase = context.A_ProductProcedureBase.FirstOrDefault(s =>
                    s.ProcedureCode == _currentTask.ProcedureCode && s.IsAvailable == true && s.PlanCode == _currentTask.PlanCode
                    && s.ProductID == _currentTask.ProductID);

                //?????类型不匹配????
                if (aProductProcedureBase != null)
                {
                    cProductProcessing.ProcedureID = aProductProcedureBase.ProcedureID.ToString();
                    cProductProcessing.ProcedureName = aProductProcedureBase.ProcedureName;
                }

                //记得修改
                cProductProcessing.EquipmentName = "热机ca1688";
                cProductProcessing.OnlineStaffID = _staffId;
                cProductProcessing.OnlineStaffCode = _staffCode;
                cProductProcessing.OnlineStaffName = _staffName;
                cProductProcessing.OnlineTime = context.GetServerDate();

                //上线类型判断

                context.C_ProductProcessing.Add(cProductProcessing);
                context.SaveChanges();


                DisplayInfoToMainPanel(cProductProcessing.ProductBornCode, cProductProcessing.ProductName,
                    cProductProcessing.ProcedureName, cProductProcessing.OnlineTime.ToString());
                ChangeBgColor();

                Close();
                Dispose();
            }
        }
        private void ProductIDTxt_DoubleClick(object sender, EventArgs e)
        {
            if (CheckProductBornCode(ProductIDTxt.Text.Trim()))
            {
                CheckTaskValidity();
            }
        }
    }
}
