using AutoMapper;
using HZH_Controls;
using HZH_Controls.Controls;
using HZH_Controls.Forms;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using QualityCheckDemo;

namespace MachineryProcessingDemo
{
    public partial class MainPanel : FrmBase
    {
        public MainPanel(long? staffId, string staffCode, string staffName)
        {
            InitializeComponent();
            _staffId = staffId;
            _staffCode = staffCode;
            _staffName = staffName;
            EmployeeIDTxt.Text = staffCode;
            EmployeeNameTxt.Text = staffName;
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
        private static int _onlyOneOnlineForm = 0; 

        //图标间宽度
        private static int _widthX = 400;
        //tuple计数器
        private static int _tupleI = 0;
        //全局静态只读tuple
        private static readonly List<Tuple<string, string>> MuneList = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("工量具", "E_icon_tools"),
            // new Tuple<string, string>("工量具", "A_fa_wrench"),
            // new Tuple<string, string>("扫描枪状态", "A_fa_check_circle"),
            new Tuple<string, string>("扫描枪状态", "E_icon_check_alt2"),
            // new Tuple<string, string>("上线", "E_arrow_carrot_2up_alt"),
            // new Tuple<string, string>("程序文件", "A_fa_file_text"),
            new Tuple<string, string>("程序文件", "A_fa_list_alt"),
            new Tuple<string, string>("作业指导", "A_fa_github"),
            // new Tuple<string, string>("自检项录入", "A_fa_edit"),
            // new Tuple<string, string>("自检项录入", "E_icon_pencil_edit"),
            // new Tuple<string, string>("下线", "E_arrow_carrot_2down_alt2"),
            new Tuple<string, string>("强制下线", "A_fa_stack_overflow"),
            new Tuple<string, string>("强制下线", "E_icon_error_circle_alt"),
            new Tuple<string, string>("切换账号", "E_arrow_left_right_alt"),
            new Tuple<string, string>("退出", "A_fa_power_off"),
            // new Tuple<string, string>("人员信息", "A_fa_address_card_o"),
        };

        private static C_ProductProcessing _cProductProcessing;
        private void MainPanel_Load(object sender, EventArgs e)
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

            //使用hzh控件自带的图标库 tuple

            //解析tuple 加载顶部菜单栏 绑定事件
            var workersGaugeLabel = GenerateLabel();
            workersGaugeLabel.Click += lbl_DoubleClick;

            var scanGunStateLabel = GenerateLabel();
            scanGunStateLabel.DoubleClick += lbl_DoubleClick;

            // var onlineLabel = GenerateLabel();
            // onlineLabel.Click += OpenScanOnlineForm;

            var programFilesLabel = GenerateLabel();
            programFilesLabel.Click += OpenProgramFile;

            var workInstructionLabel = GenerateLabel();
            workInstructionLabel.Click += OpenWorkInstruction;

            // var selfCheckItemInputLabel = GenerateLabel();
            // selfCheckItemInputLabel.Click += OpenSelfCheckItemForm;

            // var offlineLabel = GenerateLabel();
            // offlineLabel.Click += OpenScanOfflineForm;

            var forceOfflineLabel = GenerateLabel();
            forceOfflineLabel.Click += OpenForceOfflineForm;

            var forceOfflineLabel1 = GenerateLabel();
            forceOfflineLabel1.Click += OpenForceOfflineForm; 

            var switchAccountLabel = GenerateLabel();
            switchAccountLabel.Click += OpenLoginForm;

            var exitLabel = GenerateLabel();
            exitLabel.Click += CloseForms;

            // 加载人员信息图标
            var tuple1 = new Tuple<string, string>("人员信息", "A_fa_address_card_o");
            var icon1 = (FontIcons)Enum.Parse(typeof(FontIcons), tuple1.Item2);
            var pictureBox1 = new PictureBox
            {
                AutoSize = false,
                Size = new Size(240, 160),
                ForeColor = Color.FromArgb(255, 77, 59),
                Image = FontImages.GetImage(icon1, 32, Color.FromArgb(255, 77, 59)),
                Location = new Point(110, 20)
            };
            PersonnelInfoPanel.Controls.Add(pictureBox1);

            // 加载箭头图标
            var tuple2 = new Tuple<string, string>("Arrow", "A_fa_arrow_down");
            var icon2 = (FontIcons)Enum.Parse(typeof(FontIcons), tuple2.Item2);
            int localY = 72;
            for (var i = 0; i < 1; i++)
            {
                ProductionStatusInfoPanel.Controls.Add(new PictureBox()
                {
                    AutoSize = false,
                    Size = new Size(40, 40),
                    ForeColor = Color.FromArgb(255, 77, 59),
                    Image = FontImages.GetImage(icon2, 40, Color.FromArgb(255, 77, 59)),
                    Location = new Point(270, localY)
                });
                localY += 98;
            }

            //修改自定义控件label.text文本
            CompletedTask.label1.Text = " 已完成任务";
            ProductionTaskQueue.label1.Text = "热压任务队列";

            InitialDidTasks();

            ucSignalLamp1.LampColor = new Color[] { Color.Green };
            ucSignalLamp2.LampColor = new Color[] { Color.Red };

            InialToDoTasks();

            //初始化生产状态信息面板
            using (var context = new Model())
            {
                //这里需要配置修改xml
                var cBBdbRCntlPntBases = context.C_BBdbR_CntlPntBase.Where(s =>
                        s.CntlPntTyp =="3" && s.Enabled == 1.ToString())
                    .OrderBy(s => s.CntlPntSort).ToList();

                int localLblY = 25;
                foreach (var cBBdbRCntlPntBase in cBBdbRCntlPntBases)
                {
                    var label = new Label()
                    {
                        Location = new Point(239, localLblY),
                        Size = new Size(112, 39),
                        Name = cBBdbRCntlPntBase.CntlPntCd,
                        BackColor = Color.LightSlateGray,
                        Font = new Font("微软雅黑", 10.8F, FontStyle.Bold,
                            GraphicsUnit.Point, ((byte)(134))),
                        Text = cBBdbRCntlPntBase.CntlPntNm,
                        TextAlign = ContentAlignment.MiddleCenter,
                    };
                    if (label.Name.Equals("control001"))
                    {
                        label.Click += ProductOnlineEvent;
                    }
                    // else if (label.Name.Equals("control002"))
                    // {
                        // label.Click += SelfCheckItemEvent;
                    // }
                    else if (label.Name.Equals("control002"))
                    {
                        label.Click += ProductOfflineEvent;
                    }
                    ProductionStatusInfoPanel.Controls.Add(label);
                    localLblY += 96;
                }
            }

            //获取当前加工中心的生产任务(已上线)
            using (var context = new Model())
            {
                var cProductProcessing = context.C_ProductProcessing
                    .FirstOrDefault(s => s.WorkshopCode == _workshopCode && s.OnlineTime != null);
                if (cProductProcessing != null)
                {
                    ProductIDTxt.Text = cProductProcessing.ProductBornCode;
                    ProductIDTxt.ReadOnly = true;
                    ProductNameTxt.Text = cProductProcessing.ProductName;
                    ProductNameTxt.ReadOnly = true;
                    CurrentProcessTxt.Text = cProductProcessing.ProcedureName;
                    CurrentProcessTxt.ReadOnly = true;
                    OnlineTimeTxt.Text = cProductProcessing.OnlineTime.ToString();
                    OnlineTimeTxt.ReadOnly = true;
                    // ProductOnlineLbl.BackColor = Color.MediumSeaGreen;
                    ProductionStatusInfoPanel.Controls.Find("control001", false).First().BackColor =
                        Color.MediumSeaGreen;
                }

                if (!string.IsNullOrEmpty(ProductIDTxt.Text))
                {
                    //在产品加工过程表中根据产品出生证  获取元数据
                    _cProductProcessing = context.C_ProductProcessing.FirstOrDefault(s => s.ProductBornCode == ProductIDTxt.Text.Trim());
                }
            }

            timer1.Enabled = true; 
        }
        private void OpenForceOfflineForm(object sender ,EventArgs e )
        {
        }
        public void InialToDoTasks()
        {
            //  自定义表格 装载图片等资源
            List<DataGridViewColumnEntity> lstColumns1 = new List<DataGridViewColumnEntity>
            {
                new DataGridViewColumnEntity()
                {
                    DataField = "ProductBornCode", HeadText = "产品出生证", Width = 40, WidthType = SizeType.Percent
                },
                new DataGridViewColumnEntity()
                {
                    DataField = "Reserve2", HeadText = "工序名称", Width = 15, WidthType = SizeType.Percent
                },
                new DataGridViewColumnEntity()
                {
                    DataField = "CreateTime", HeadText = "预计开始时间", Width = 20, WidthType = SizeType.Percent
                },
                new DataGridViewColumnEntity()
                {
                    DataField = "Reserve1", HeadText = "工序状态", Width = 25, WidthType = SizeType.Percent
                }
            };
            ucDataGridView2.Columns = lstColumns1;
            ucDataGridView2.ItemClick += UcDataGridView2_ItemClick;
            //拿到待加工产品排序集合
            var apsProcedureTaskDetails = GetToDoProcedureTask();
            ucDataGridView2.DataSource = apsProcedureTaskDetails;
        }
        public void InitialDidTasks()
        {
            // 自定义表格 装载图片等资源
            List<DataGridViewColumnEntity> lstColumns = new List<DataGridViewColumnEntity>
            {
                new DataGridViewColumnEntity()
                {
                    DataField = "ProductBornCode", HeadText = "产品出生证", Width = 35, WidthType = SizeType.Percent
                },
                new DataGridViewColumnEntity()
                {
                    DataField = "Reserve2", HeadText = "工序名称", Width = 15, WidthType = SizeType.Percent
                },
                new DataGridViewColumnEntity()
                {
                    DataField = "Reserve3", HeadText = "下机类型", Width = 20, WidthType = SizeType.Percent
                },
                new DataGridViewColumnEntity()
                {
                    DataField = "Reserve1", HeadText = "工序状态", Width = 25, WidthType = SizeType.Percent
                }
            };

            var didProcedureTask = GetDidProcedureTask();
            ucDataGridView1.Columns = lstColumns;
            this.ucDataGridView1.DataSource = didProcedureTask;
        }
        private void UcDataGridView2_ItemClick(object sender, DataGridViewEventArgs e)
        {
            ProductionStatusInfoPanel.Controls.Find("control001", false).First().BackColor =
                Color.LightSlateGray;
            ProductionStatusInfoPanel.Controls.Find("control002", false).First().BackColor =
                Color.LightSlateGray;

            var controls = panel10.Controls.Find("scanOnlineForm",false);
            if (controls.Any())
            {
                controls[0].Dispose();
            }

            if (!HasExitProductTask())
            {
                ProductNameTxt.Clear();
                ProductIDTxt.Clear();
                CurrentProcessTxt.Clear();
                OnlineTimeTxt.Clear();

                var dataGridViewRow = ucDataGridView2.SelectRow;
                var dataSource = dataGridViewRow.DataSource;
                if (dataSource is APS_ProcedureTaskDetail apsProcedureTaskDetail)
                {
                    var dialogResult = FrmDialog.ShowDialog(this, $"确定上线选中产品[{apsProcedureTaskDetail.ProductBornCode}]吗", "热压上线", true);
                    if (dialogResult == DialogResult.OK)
                    {
                        var scanOnlineForm = new ScanOnlineForm(_staffId, _staffCode, _staffName, apsProcedureTaskDetail.ProductBornCode, _workshopId, _workshopCode, _workshopName, _equipmentId, _equipmentCode, _equipmentName)
                        {
                            DisplayInfoToMainPanel = (s1, s2, s3, s4) =>
                            {
                                ProductIDTxt.Text = s1;
                                ProductNameTxt.Text = s2;
                                CurrentProcessTxt.Text = s3;
                                OnlineTimeTxt.Text = s4;
                            },
                            ChangeBgColor = () =>
                            {
                                ProductionStatusInfoPanel.Controls.Find("control001", false).First().BackColor =
                                    Color.MediumSeaGreen;
                                ProductionStatusInfoPanel.Controls.Find("control002", false).First().BackColor =
                                    Color.LightSlateGray;
                            }
                        };

                        if (scanOnlineForm.CheckTaskValidity(apsProcedureTaskDetail.ProcedureCode))
                        {
                            //判断机加工任务是否全部完成
                        var doneMachiningOrNot = DoneMachiningOrNot(apsProcedureTaskDetail); 
                        if (!doneMachiningOrNot)
                        {
                            FrmDialog.ShowDialog(this, "尚有机加工环节未执行,请先执行机加工任务!");
                            return; 
                        }
                            scanOnlineForm.AddCntLogicPro();
                            //先判断一下本产品出生证的有没有待检验的前序质检任务没做
                            // var hasSelfQcTask = scanOnlineForm.HasSelfQcTask();
                            
                            //转档  工序任务明细表=>产品加工过程表
                                    scanOnlineForm.ProcessTurnArchives();
                                    //完善工序任务明细表中的数据 诸如任务状态 ; 修改人修改时间
                                    scanOnlineForm.PerfectApsDetail();
                                    //完善计划产品出生证表
                                    // scanOnlineForm.PerfectPlanProductInfo();
                                    //转档  
                                    scanOnlineForm.CntLogicTurn();
                                    FrmDialog.ShowDialog(this, $"产品{ProductIDTxt.Text}热压上线成功!", "热压上线成功");

                                    InialToDoTasks();
                        }
                    }
                }
            }
            else
            {
                ProductionStatusInfoPanel.Controls.Find("control001", false).First().BackColor =
                    Color.MediumSeaGreen;
            }
        }

        private bool DoneMachiningOrNot(APS_ProcedureTaskDetail apsProcedureTaskDetail)
        {
            using (var context = new Model())
            {
                var apsProcedureTaskDetails = context.APS_ProcedureTaskDetail.Where(s =>
                    s.ProcedureType == (decimal?) ProcedureType.Machining &&
                    s.ProductBornCode == apsProcedureTaskDetail.ProductBornCode).ToList();
                if (apsProcedureTaskDetails.All(s=> s.TaskState == (decimal?)ApsProcedureTaskDetailState.Completed))
                {
                    return true; 
                }
                return false; 
            }
        }

        private List<APS_ProcedureTaskDetail> GetDidProcedureTask()
        {
            var procedureTaskDetails = new List<APS_ProcedureTaskDetail>();
            using (var context = new Model())
            {
                //在工序任务表中根据设备编号和 开始时间排序得到优先级最高的工序任务
                var apsProcedureTasks = context.APS_ProcedureTask.Where(s =>
                        s.EquipmentID.ToString() == _equipmentId && s.IsAvailable == true &&
                        s.ProcedureType == (decimal?) ProcedureType.HotPressing)
                    .OrderBy(s => s.StartTime).ToList();
                foreach (var apsProcedureTask in apsProcedureTasks)
                {
                    //在工序明细表中 根据tasktableid/设备号/工序号/有效性/任务状态 获取已完成的任务集合(排序)
                    var apsProcedureTaskDetails = context.APS_ProcedureTaskDetail.Where(s =>
                            s.EquipmentID == apsProcedureTask.EquipmentID &&
                            s.ProcedureCode == apsProcedureTask.ProcedureCode && s.IsAvailable == true &&
                            s.ProcedureType == (decimal?) ProcedureType.HotPressing &&
                            s.TaskTableID == apsProcedureTask.ID &&
                            s.TaskState == (decimal?) ApsProcedureTaskDetailState.Completed)
                        .OrderBy(s => s.LastModifiedTime);

                    foreach (var apsProcedureTaskDetail in apsProcedureTaskDetails)
                    {
                        //令apsdetail的reserve2 字段作为工序名称来搞
                        //在产品工序基础表中根据产品号/有效性  获得工序名称
                        apsProcedureTaskDetail.Reserve2 = context.A_ProductProcedureBase.Where(s =>
                                s.IsAvailable == true && s.ProductID == apsProcedureTask.ProductID &&
                                s.PlanCode == apsProcedureTask.PlanCode &&
                                s.ProcedureCode == apsProcedureTaskDetail.ProcedureCode).Select(s => s.ProcedureName)
                            .FirstOrDefault();

                        //reserve3作为下机类型
                        var cProductProcessingDocument = context.C_ProductProcessingDocument.FirstOrDefault(s =>
                            s.ProductBornCode == apsProcedureTaskDetail.ProductBornCode &&
                            s.ProcedureCode == apsProcedureTaskDetail.ProcedureCode);
                        apsProcedureTaskDetail.Reserve3 =
                            cProductProcessingDocument.Offline_type == (decimal?)ProductProcessingOfflineType.Normal
                                ?
                                "正常下机"
                                : cProductProcessingDocument.Offline_type ==
                                  (decimal?)ProductProcessingOfflineType.Force
                                    ? "强制下机"
                                    : "不良下机";

                        //reserve1 判断当前产品当前工序的状态 待加工/正在加工/加工完毕/待送检(三坐标/手检)/正在送检(三坐标/手检)/送检完毕(三坐标/手检)
                        if (apsProcedureTaskDetail.TaskState == (decimal?) ApsProcedureTaskDetailState.NotOnline)
                        {
                            apsProcedureTaskDetail.Reserve1 = "待加工";
                        }
                        else if (apsProcedureTaskDetail.TaskState ==
                                 (decimal?) ApsProcedureTaskDetailState.InExcecution)
                        {
                            apsProcedureTaskDetail.Reserve1 = "正在加工";
                        }
                        else if (apsProcedureTaskDetail.TaskState == (decimal?) ApsProcedureTaskDetailState.Completed)
                        {
                            if (apsProcedureTaskDetail.IsInspect != 1)
                            {
                                apsProcedureTaskDetail.Reserve1 = "加工完毕";
                            }
                            else
                            {
                                //获得三坐标检验任务
                                var cCheckTask = context.C_CheckTask.FirstOrDefault(s =>
                                    s.ProductBornCode == apsProcedureTaskDetail.ProductBornCode &&
                                    s.ProcedureCode == apsProcedureTaskDetail.ProcedureCode && s.IsAvailable == true);
                                apsProcedureTaskDetail.Reserve1 =
                                    cCheckTask.TaskState == (decimal?) CheckTaskState.NotOnline ? "待送检(三坐标)" :
                                    cCheckTask.TaskState == (decimal?) CheckTaskState.InExecution ? "正在送检(三坐标)" :
                                    "送检完毕(三坐标)";
                            }
                        }
                        procedureTaskDetails.Add(apsProcedureTaskDetail);
                    }
                    
                }
            }

            return procedureTaskDetails;
        }
        private List<APS_ProcedureTaskDetail> GetToDoProcedureTask()
        {
            var procedureTaskDetails = new List<APS_ProcedureTaskDetail>();
            using (var context = new Model())
            {
                //在工序任务表中根据设备编号和任务状态和 开始时间排序得到优先级最高的工序任务
                var apsProcedureTasks = context.APS_ProcedureTask.Where(s =>
                        s.EquipmentID.ToString() == _equipmentId && s.TaskState == (decimal?) ApsProcedureTaskState.ToDo
                        &&s.IsAvailable ==true )
                    .OrderBy(s => s.StartTime).ToList();
                foreach (var apsProcedureTask in apsProcedureTasks)
                {
                    //在工序任务明细表中 根据tasktableid/设备号/工序号/有效性/任务状态 获取待完成的任务集合(排序)
                    var apsProcedureTaskDetails = context.APS_ProcedureTaskDetail.Where(s =>
                            s.EquipmentID == apsProcedureTask.EquipmentID &&
                            s.ProcedureCode == apsProcedureTask.ProcedureCode && s.TaskState != (decimal?) ApsProcedureTaskDetailState.Completed &&
                            s.ProcedureType==(decimal?) ProcedureType.HotPressing&&
                            s.IsAvailable == true && s.TaskTableID == apsProcedureTask.ID).OrderByDescending(s=>s.TaskState)
                        .ToList();

                    foreach (var apsProcedureTaskDetail in apsProcedureTaskDetails)
                    {
                        //令apsdetail的reserve2 字段作为工序名称来搞
                        //在产品工序基础表中根据产品号/有效性  获得工序名称
                        apsProcedureTaskDetail.Reserve2 = context.A_ProductProcedureBase.Where(s =>
                            s.IsAvailable == true && s.ProductID == apsProcedureTask.ProductID &&
                            s.PlanCode == apsProcedureTask.PlanCode &&
                            s.ProcedureCode == apsProcedureTaskDetail.ProcedureCode).Select(s => s.ProcedureName)
                            .FirstOrDefault();

                        apsProcedureTaskDetail.Reserve1 =
                            apsProcedureTaskDetail.TaskState == (decimal?) ApsProcedureTaskDetailState.NotOnline
                                ? "待热压"
                                : apsProcedureTaskDetail.TaskState ==
                                  (decimal?) ApsProcedureTaskDetailState.InExcecution
                                    ? "正在热压"
                                    : "热压完毕";

                        procedureTaskDetails.Add(apsProcedureTaskDetail);
                    }
                }
            }
            return procedureTaskDetails;
        }

        private bool HasExitProductTask()
        {
            using (var context = new Model())
            {
                //在产品加工过程表中根据加工中心编号和上线时间非空  判断本加工中心是否有正在处理的生产任务 
                var cProductProcessing = context.C_ProductProcessing
                    .FirstOrDefault(s => s.EquipmentID.ToString() == _equipmentId && s.OnlineTime != null);
                if (cProductProcessing != null)
                {
                    FrmDialog.ShowDialog(this, "当前已有正在处理的生产任务,请完成", "已有生产任务");
                    return true;
                }
                    return false;
            }
        }
        private void ProductOnlineEvent(object sender, EventArgs e)
        {
            var find = panel10.Controls.Find("scanOnlineForm",false);
            if (find.Any())
            {
                return;
            }
            ProductionStatusInfoPanel.Controls.Find("control001", false).First().BackColor =
                Color.LightSlateGray;
            ProductionStatusInfoPanel.Controls.Find("control002", false).First().BackColor =
                Color.LightSlateGray;
            var exitProductTask = HasExitProductTask();
            if (!exitProductTask)
            {
                ProductNameTxt.Clear();
                ProductIDTxt.Clear();
                CurrentProcessTxt.Clear();
                OnlineTimeTxt.Clear();
                var scanOnlineForm = new ScanOnlineForm(_staffId, _staffCode, _staffName)
                    {
                        DisplayInfoToMainPanel = (s1, s2, s3, s4) =>
                        {
                            ProductIDTxt.Text = s1;
                            ProductNameTxt.Text = s2;
                            CurrentProcessTxt.Text = s3;
                            OnlineTimeTxt.Text = s4;
                        },
                        ChangeBgColor = () =>
                        {
                            ProductionStatusInfoPanel.Controls.Find("control001", false).First().BackColor =
                                    Color.MediumSeaGreen;
                            ProductionStatusInfoPanel.Controls.Find("control002", false).First().BackColor =
                                Color.LightSlateGray;
                        },
                        RegetProcedureTasksDetails =InialToDoTasks,
                    ClearMainPanelTxt= () =>
                    {
                        ProductIDTxt.Clear();
                        ProductIDTxt.ReadOnly = false;
                        ProductNameTxt.Clear();
                        ProductNameTxt.ReadOnly = false;
                        CurrentProcessTxt.Clear();
                        CurrentProcessTxt.ReadOnly = false;
                        OnlineTimeTxt.Clear();
                        OnlineTimeTxt.ReadOnly = false;
                        ProductionStatusInfoPanel.Controls.Find("control001", false).First().BackColor =
                            Color.LightSlateGray;
                    }
                };
                    var controls = scanOnlineForm.Controls.Find("lblTitle", false).First();
                    controls.Visible = false;
                    scanOnlineForm.Location = new Point(panel10.Width / 2 - scanOnlineForm.Width / 2, 0);
                    scanOnlineForm.FormBorderStyle = FormBorderStyle.None;
                    scanOnlineForm.AutoSize = false;
                    scanOnlineForm.AutoScaleMode = AutoScaleMode.None;
                    scanOnlineForm.Size = new Size(553, panel10.Height);
                    scanOnlineForm.AutoScaleMode = AutoScaleMode.Font;
                    scanOnlineForm.TopLevel = false;
                    scanOnlineForm.BackColor = Color.FromArgb(247, 247, 247);
                    scanOnlineForm.ForeColor = Color.FromArgb(66, 66, 66);
                    panel10.Controls.Add(scanOnlineForm);
                    scanOnlineForm.Show();
            }
            else
            {
                ProductionStatusInfoPanel.Controls.Find("control001", false).First().BackColor =
                    Color.MediumSeaGreen;
            }
        }
        private void ProductOfflineEvent(object sender, EventArgs e)
        {
            panel10.Controls.Clear();

            if (string.IsNullOrEmpty(ProductIDTxt.Text))
            {
                FrmDialog.ShowDialog(this, "未检测到上线产品", "警告");
                return;
            }
                OpenScanOfflineForm(out var isOk);
                if (isOk)
                {
                    AddCntLogicProOffline();
                }
        }
        private void AddCntLogicProOffline(string remark = "")
        {
            using (var context = new Model())
            {
                //在产品加工过程表中根据产品出生证  获取元数据
                _cProductProcessing = context.C_ProductProcessing.FirstOrDefault(s => s.ProductBornCode == ProductIDTxt.Text.Trim());
                    var cBWuECntlLogicPro = new C_BWuE_CntlLogicPro
                {
                    ProductBornCode = ProductIDTxt.Text.Trim(),
                    ProcedureCode = _cProductProcessing.ProcedureCode,
                    ControlPointID =8 ,
                    Sort = "2",
                    EquipmentCode = _equipmentCode,
                    State = "1",
                    StartTime = context.GetServerDate() , 
                    Remarks = remark 
                };

                context.Entry(cBWuECntlLogicPro).State = EntityState.Added;
                context.SaveChanges();
            }
        }
        private void OpenScanOfflineForm(out bool isOk)
        {
            var scanOfflineForm = new ScanOfflineForm(ProductIDTxt.Text.Trim(), _staffId, _staffCode, _staffName)
            {
                ChangeBgColor = () =>
                    ProductionStatusInfoPanel.Controls.
                        Find("control002", false).First().BackColor = Color.MediumSeaGreen,
                ClearMainPanelTxt = () =>
                {
                    ProductIDTxt.Clear();
                    CurrentProcessTxt.Clear();
                    ProductNameTxt.Clear();
                    OnlineTimeTxt.Clear();
                },
                RegetProcedureTasksDetails = () =>
                {
                    InitialDidTasks();
                    InialToDoTasks();
                }
            };
            var controls = scanOfflineForm.Controls.Find("lblTitle", false).First();
            controls.Visible = false;
            scanOfflineForm.Location = new Point(panel10.Width / 2 - scanOfflineForm.Width / 2, 0);
            scanOfflineForm.FormBorderStyle = FormBorderStyle.None;
            scanOfflineForm.AutoSize = false;
            scanOfflineForm.AutoScaleMode = AutoScaleMode.None;
            scanOfflineForm.Size = new Size(553, panel10.Height);
            scanOfflineForm.AutoScaleMode = AutoScaleMode.Font;
            scanOfflineForm.TopLevel = false;
            scanOfflineForm.BackColor = Color.FromArgb(247, 247, 247);
            scanOfflineForm.ForeColor = Color.FromArgb(66, 66, 66);
            panel10.Controls.Add(scanOfflineForm);
            scanOfflineForm.Show();
            isOk = true;
        }
        private void OpenWorkInstruction(object sender, EventArgs e)
        {
            //这里需要修改 根据指定的路径规则 查找作业指导书
            System.Diagnostics.Process.Start("C:\\Users\\Sweetie\\Desktop\\车间级制造执行系统IDEF图_第一层+第二层+第三层 - v1.9.vsdx");
        }
        private void OpenProgramFile(object sender, EventArgs e)
        {
            //这里需要修改 根据指定的路径规则 查找并下载机床程序文件
            System.Diagnostics.Process.Start("C:\\Users\\Sweetie\\Desktop\\20-033班审批材料补交\\2020170277-肖锴.pdf");
        }
        private void OpenLoginForm(object sender, EventArgs e)
        {
            new UserLoginForm().Show();

            C_LoginInProcessing cLoginInProcessing;
            using (var context = new Model())
            {
                //没用所以没改
                cLoginInProcessing = context.C_LoginInProcessing.First(s => s.StaffCode == EmployeeIDTxt.Text && s.EquipmentID == 1 && s.OfflineTime == null);
                cLoginInProcessing.OfflineTime = context.GetServerDate();
                context.SaveChanges();
            }
            LoginUserTurnArchives(cLoginInProcessing);

            _tupleI = 0;
            _widthX = 400;
            this.Close();
        }
        private void CloseForms(object sender, EventArgs e)
        {
            // C_LoginInProcessing cLoginInProcessing;
            // using (var context = new Model())
            // {
            //     cLoginInProcessing = context.C_LoginInProcessing.First(s => s.StaffCode == EmployeeIDTxt.Text && s.EquipmentID == 4 && s.OfflineTime == null);
            //     cLoginInProcessing.OfflineTime = context.GetServerDate();
            //     context.SaveChanges();
            // }
            // LoginUserTurnArchives(cLoginInProcessing);
            Application.Exit();
        }
        private void lbl_DoubleClick(object sender, EventArgs e)
        {
            MessageBox.Show("hello world");
        }
        private Label GenerateLabel()
        {
            var icon = (FontIcons)Enum.Parse(typeof(FontIcons), MuneList[_tupleI].Item2);
            var label = new Label
            {
                AutoSize = false,
                Size = new Size(90, 60),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.BottomCenter,
                ImageAlign = ContentAlignment.TopCenter,
                Margin = new Padding(5),
                Text = MuneList[_tupleI].Item1,
                Image = FontImages.GetImage(icon, 32, Color.White),
                Location = new Point(_widthX, 0),
                Font = new Font("微软雅黑", 12, FontStyle.Bold)
            };
            FirstTitlePanel.Controls.Add(label);
            _widthX += 90;
            _tupleI++;
            return label;
        }
        public void LoginUserTurnArchives(C_LoginInProcessing cLoginInProcessing)
        {
            var mapperConfiguration = new MapperConfiguration(cfg => cfg.CreateMap<C_LoginInProcessing, C_LoginInDocument>());
            var mapper = mapperConfiguration.CreateMapper();
            var cLoginInDocument = mapper.Map<C_LoginInDocument>(cLoginInProcessing);
            using (var context = new Model())
            {
                //此处应该优化成事务操作 保证acid原则
                context.C_LoginInDocument.Add(cLoginInDocument);
                context.SaveChanges();
                context.Entry(cLoginInProcessing).State = EntityState.Deleted;
                // context.C_LoginInProcessing.Remove(cLoginInProcessing);
                context.SaveChanges();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            var apsProcedureTaskDetails = GetDidProcedureTask();
            ucDataGridView1.DataSource = apsProcedureTaskDetails; 
        }
    }
    public class TestGridModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
        public int Sex { get; set; }
        public int Age { get; set; }
        public List<TestGridModel> Childrens { get; set; }
    }
}
