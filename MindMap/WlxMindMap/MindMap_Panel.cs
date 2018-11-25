﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using WlxMindMap.MindMapNode;
using WlxMindMap.MindMapNodeContent;

namespace WlxMindMap
{
    public partial class MindMap_Panel : UserControl
    {
        private MindMapNode.MindMapNodeContainer mindMapNode= new WlxMindMap.MindMapNode.MindMapNodeContainer();
          
        public MindMap_Panel()
        {
            InitializeComponent();
            #region 根节点容器

            // 
            // mindMapNode
            // 
            this.mindMapNode.BackColor = System.Drawing.Color.White;
            this.mindMapNode.Location = new System.Drawing.Point(181, 166);            
            this.mindMapNode.Name = "mindMapNode";
            this.mindMapNode.ParentNode = null;         
            this.mindMapNode.Size = new System.Drawing.Size(86, 23);
            this.mindMapNode.TabIndex = 0;           
            this.mindMapNode.EmptyRangeClick += new System.EventHandler(this.mindMapNode_EmptyRangeClick);
            this.mindMapNode.EmptyRangeMouseDown += new System.Windows.Forms.MouseEventHandler(this.mindMapNode_EmptyRangeMouseDown);
            this.mindMapNode.EmptyRangeMouseUp += new System.Windows.Forms.MouseEventHandler(this.mindMapNode_EmptyRangeMouseUp);
            this.mindMapNode.EmptyRangeMouseMove += new System.Windows.Forms.MouseEventHandler(this.mindMapNode_EmptyRangeMouseMove);
            this.mindMapNode.Resize += new System.EventHandler(this.mindMapNode_Resize);
            this.Scroll_panel.Controls.Add(this.mindMapNode);
            #endregion 根节点容器
            this.MouseWheel += new MouseEventHandler(OnMouseWhell);
            
        }      
        public MindMapNodeStructBase DataStruct { get; set; }

        #region 公开方法

        /// <summary> 为思维导图载入数据
        /// 
        /// </summary>
        /// <typeparam name="NodeContent">采用哪种内容布局</typeparam>
        /// <typeparam name="DataEntity">数据的模型</typeparam>
        /// <param name="DataSource"></param>
        public void SetDataSource<NodeContent, DataEntity>(List<DataEntity> DataSource) where NodeContent : MindMapNodeContentBase, new()
        {
            if (DataStruct == null) throw new Exception("DataStruct为空：你需要先指定数据源的结构，再绑定数据源");
            PropertyInfo IDProperty = typeof(DataEntity).GetProperty(DataStruct.MindMapID);
            PropertyInfo ParentProperty = typeof(DataEntity).GetProperty(DataStruct.MindMapParentID);
            //没有父节点就取父节点为空的记录
            List<DataEntity> CurrentAddList = DataSource.Where(T1 => string.IsNullOrEmpty(ParentProperty.GetValue(T1).ToString())).ToList();


            if (CurrentAddList.Count == 0) throw new Exception ("未找到根节点");
            if (CurrentAddList.Count > 1) throw new Exception("不允许有多个根节点");

            string CurrentId = IDProperty.GetValue(CurrentAddList[0]).ToString();
            List<MindMapNodeContainer> ContainerList = SetDataSource<NodeContent, DataEntity>(DataSource, CurrentId);
            ContainerList.ForEach(item => mindMapNode.AddNode(item));
            mindMapNode.SetNodeContent<NodeContent>(DataStruct);
            mindMapNode.NodeContent.DataItem = CurrentAddList[0];

            SetEvent(mindMapNode);

        }

        private List<MindMapNodeContainer> SetDataSource<NodeContent, DataEntity>(List<DataEntity> DataSource, string ParentID) where NodeContent : MindMapNodeContentBase, new()
        {
            PropertyInfo IDProperty = typeof(DataEntity).GetProperty(DataStruct.MindMapID);
            PropertyInfo ParentProperty = typeof(DataEntity).GetProperty(DataStruct.MindMapParentID);
            //有父节点就取ParentID为父节点的记录
            List<DataEntity> CurrentAddList = DataSource.Where(T1 => ParentProperty.GetValue(T1).ToString() == ParentID).ToList();
            List<MindMapNodeContainer> ContainerList = new List<MindMapNodeContainer>();

            foreach (DataEntity AddDataItem in CurrentAddList)
            {
                string CurrentId = IDProperty.GetValue(AddDataItem).ToString();
                List<MindMapNodeContainer> ContainerListTemp = SetDataSource<NodeContent, DataEntity>(DataSource, CurrentId);
                MindMapNodeContainer NewNode = new MindMapNodeContainer ();
                NewNode.SetNodeContent<NodeContent>(DataStruct);
                ContainerListTemp.ForEach(item => NewNode.AddNode(item));              
                NewNode.NodeContent.DataItem = AddDataItem;
                ContainerList.Add(NewNode);
            }
            return ContainerList;
        }

        /// <summary> 获取所有被选中的节点
        /// 
        /// </summary>
        /// <returns></returns>
        public List<MindMapNode.MindMapNodeContainer> GetSelectedNode()
        {
            List<MindMapNode.MindMapNodeContainer> ResultList = new List<MindMapNode.MindMapNodeContainer>();
            ResultList = mindMapNode.GetChidrenNode(true);
            ResultList.Add(mindMapNode);
            ResultList = ResultList.Where(T1 => T1.NodeContent.Selected == true).ToList();
            return ResultList;
        }

        #endregion 公开方法


        #region 公开事件委托
        private void SetEvent(MindMapNodeContainer MindMapContainerParame)
        {
            #region 为节点容器添加事件
            //节点容器添加事件
            List<MindMapNodeContainer> NodeContainsList = MindMapContainerParame.GetChidrenNode(true);//获取所有节点容器
            NodeContainsList.Add(mindMapNode);//包括自己

            List<Control> NodeContentList = new List<Control>();//用List来收集所有节点内容的控件
            NodeContainsList.ForEach(NodeItem =>
            {
                //避免重复添加委托队列
                NodeItem.EmptyRangeClick -= new EventHandler(mindMapNode_EmptyRangeClick);
                NodeItem.EmptyRangeMouseDown -= new MouseEventHandler(mindMapNode_EmptyRangeMouseDown);
                NodeItem.EmptyRangeMouseMove -= new MouseEventHandler(mindMapNode_EmptyRangeMouseMove);
                NodeItem.EmptyRangeMouseUp -= new MouseEventHandler(mindMapNode_EmptyRangeMouseUp);

                NodeItem.EmptyRangeClick += new EventHandler(mindMapNode_EmptyRangeClick);
                NodeItem.EmptyRangeMouseDown += new MouseEventHandler(mindMapNode_EmptyRangeMouseDown);
                NodeItem.EmptyRangeMouseMove += new MouseEventHandler(mindMapNode_EmptyRangeMouseMove);
                NodeItem.EmptyRangeMouseUp += new MouseEventHandler(mindMapNode_EmptyRangeMouseUp);

                NodeContentList.AddRange(NodeItem.NodeContent.GetNodeControl());//获取当前节点内容的所有控件
            });
            #endregion 为节点容器添加事件

            #region 为节点内容添加事件
            NodeContentList.ForEach(ControlItem =>
                {
                //避免重复添加委托队列
                ControlItem.MouseDown -= new MouseEventHandler(mindMapNode_MindMapNodeMouseDown);
                    ControlItem.MouseUp -= new MouseEventHandler(mindMapNode_MindMapNodeMouseUp);
                    ControlItem.MouseMove -= new MouseEventHandler(mindMapNode_MindMapNodeMouseMove);
                    ControlItem.MouseEnter -= new EventHandler(mindMapNode_MindMapNodeMouseEnter);
                    ControlItem.MouseLeave -= new EventHandler(mindMapNode_MindMapNodeMouseLeave);
                    ControlItem.MouseClick -= new MouseEventHandler(mindMapNode_MindMapNodeMouseClick);
                    ControlItem.MouseDoubleClick -= new MouseEventHandler(mindMapNode_MouseDoubleClick);

                    ControlItem.MouseDown += new MouseEventHandler(mindMapNode_MindMapNodeMouseDown);
                    ControlItem.MouseUp += new MouseEventHandler(mindMapNode_MindMapNodeMouseUp);
                    ControlItem.MouseMove += new MouseEventHandler(mindMapNode_MindMapNodeMouseMove);
                    ControlItem.MouseEnter += new EventHandler(mindMapNode_MindMapNodeMouseEnter);
                    ControlItem.MouseLeave += new EventHandler(mindMapNode_MindMapNodeMouseLeave);
                    ControlItem.MouseClick += new MouseEventHandler(mindMapNode_MindMapNodeMouseClick);
                    ControlItem.MouseDoubleClick += new MouseEventHandler(mindMapNode_MouseDoubleClick);
                });
            #endregion 为节点内容添加事件
        }


        /// <summary>节点被按下时
        /// 
        /// </summary>
        private void mindMapNode_MindMapNodeMouseDown(object sender, MouseEventArgs e)
        {
            MindMap_Panel_MouseDown(this, e);
            if (MindMapNodeMouseDown != null) MindMapNodeMouseDown(this, e);
        }

        /// <summary>节点在鼠标弹起时
        /// 
        /// </summary>
        private void mindMapNode_MindMapNodeMouseUp(object sender, MouseEventArgs e)
        {
            MindMap_Panel_MouseUp(this, e);
            if (MindMapNodeMouseUp != null) MindMapNodeMouseUp(this, e);
        }

        /// <summary>鼠标在节点移动时
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mindMapNode_MindMapNodeMouseMove(object sender, MouseEventArgs e)
        {
            MindMap_Panel_MouseMove(this, e);
            if (MindMapNodeMouseMove != null) MindMapNodeMouseMove(sender, e);
        }

        /// <summary>鼠标进入节点范围时
        /// 
        /// </summary>
        private void mindMapNode_MindMapNodeMouseEnter(object sender, EventArgs e)
        {
            if (MindMapNodeMouseEnter != null) MindMapNodeMouseEnter(this, e);
        }

        /// <summary>鼠标移出节点范围事件
        /// 
        /// </summary>
        private void mindMapNode_MindMapNodeMouseLeave(object sender, EventArgs e)
        {
            if (MindMapNodeMouseLeave != null) MindMapNodeMouseLeave(this, e);
        }

        /// <summary> 鼠标单击某节点
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mindMapNode_MindMapNodeMouseClick(object sender, MouseEventArgs e)
        {

            MindMapNodeContentBase SenderObject = ((Control)sender).GetNodeContent();

            if (Control.ModifierKeys != Keys.Control)//不按住ctrl就单选
            {
                List<MindMapNode.MindMapNodeContainer> MindMapNodeList = mindMapNode.GetChidrenNode(true);
                MindMapNodeList.Add(mindMapNode);
                MindMapNodeList.ForEach(T1 => T1.NodeContent.Selected = false);
                SenderObject.Selected = true;
            }
            else//按住ctrl可单选
            {
                SenderObject.Selected = !SenderObject.Selected;
            }
            if (MindMapNodeMouseClick != null) MindMapNodeMouseClick(this, e);
        }

        /// <summary> 双击某节点后编辑某节点
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mindMapNode_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (sender == null) return;
            MindMapNodeContentBase SenderObject = ((Control)sender).GetNodeContent();
            SenderObject.Edited = true;
            if (MindMapNodeMouseDoubleClick != null) MindMapNodeMouseDoubleClick(this, e);

        }

        /// <summary> 空白处被单击取消所有选中        
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mindMapNode_EmptyRangeClick(object sender, EventArgs e)
        {          
            List<MindMapNode.MindMapNodeContainer> MindMapNodeList = mindMapNode.GetChidrenNode(true);
            MindMapNodeList.Add(mindMapNode);
            foreach (MindMapNodeContainer ContainerItem in MindMapNodeList)
            {
                if (ContainerItem.NodeContent.Edited)
                {
                    ContainerItem.NodeContent.Edited = false;
                    return;
                }
            }
            MindMapNodeList.ForEach(T1 => T1.NodeContent.Selected = false);

            if (EmptyRangeClick != null) EmptyRangeClick(sender, e);



        }
               
        /// <summary> 空白处鼠标按下
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mindMapNode_EmptyRangeMouseDown(object sender, MouseEventArgs e)
        {
            MindMap_Panel_MouseDown(sender, e);
            if (EmptyRangeMouseDown != null) EmptyRangeMouseDown(sender, e);
        }

        /// <summary>空白处鼠标移动
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mindMapNode_EmptyRangeMouseMove(object sender, MouseEventArgs e)
        {
            MindMap_Panel_MouseMove(sender, e);
            if (EmptyRangeMouseMove != null) EmptyRangeMouseMove(sender, e);
        }

        /// <summary> 空白处鼠标弹起
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mindMapNode_EmptyRangeMouseUp(object sender, MouseEventArgs e)
        {
            MindMap_Panel_MouseUp(sender, e);
            if (EmptyRangeMouseUp != null) EmptyRangeMouseUp(sender, e);
        }


        /// <summary>鼠标进入节点范围事件
        /// 
        /// </summary>
        [Description("鼠标进入节点范围事件")]
        public event EventHandler MindMapNodeMouseEnter;

        /// <summary>鼠标离开节点范围事件
        /// 
        /// </summary>
        [Description("鼠标离开节点范围事件")]
        public event EventHandler MindMapNodeMouseLeave;

        /// <summary> 节点被鼠标按下事件
        /// 
        /// </summary>
        [Description("节点被鼠标按下事件")]
        public event MouseEventHandler MindMapNodeMouseDown;

        /// <summary> 节点被鼠标弹起事件
        /// 
        /// </summary>
        [Description("节点被鼠标弹起事件")]
        public event MouseEventHandler MindMapNodeMouseUp;

        /// <summary> 节点被单击时
        /// 
        /// </summary>
        [Browsable(true), Description("节点被单击时")]
        public event MouseEventHandler MindMapNodeMouseClick;

        [Browsable(true), Description("节点被双击时")]
        public event MouseEventHandler MindMapNodeMouseDoubleClick;

        /// <summary> 空白处鼠标按下
        /// 
        /// </summary>
        [Browsable(true), Description("空白处鼠标按下")]
        public event MouseEventHandler EmptyRangeMouseDown;

        /// <summary> 空白处鼠标弹起
        /// 
        /// </summary>
        [Browsable(true), Description("空白处鼠标弹起")]
        public event MouseEventHandler EmptyRangeMouseUp;

        /// <summary> 空白处鼠标移动
        /// 
        /// </summary>
        [Browsable(true), Description("空白处鼠标移动")]
        public event MouseEventHandler EmptyRangeMouseMove;

        /// <summary> 点击空白处
        /// 
        /// </summary>
        [Browsable(true), Description("点击空白处")]
        public event EventHandler EmptyRangeClick;

        /// <summary> 鼠标在节点上移动时
        /// 
        /// </summary>
        [Description("鼠标在节点上移动时")]
        public event MouseEventHandler MindMapNodeMouseMove;

        #endregion 公开事件委托    

        #region 鼠标中键拖动滚动条

        private bool IsMouseMove = false;
        private Point MoveValue = new Point();
        /// <summary> 按下中键时可拖动滚动条
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MindMap_Panel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
            {
                MoveValue = Control.MousePosition;
                IsMouseMove = true;
            }
        }

        /// <summary> 弹起中键结束拖动滚动条
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MindMap_Panel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
                IsMouseMove = false;
        }

        /// <summary>按住鼠标中间可拖动滚动条
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MindMap_Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseMove)
            {

                MoveValue.X = MoveValue.X - Control.MousePosition.X;
                MoveValue.Y = MoveValue.Y - Control.MousePosition.Y;

                Point ResultPoint = new Point(this.HorizontalScroll.Value + MoveValue.X, this.VerticalScroll.Value + MoveValue.Y);

                this.AutoScrollPosition = ResultPoint;

                MoveValue = Control.MousePosition;


            }
        }



        #endregion 鼠标中键拖动滚动条

        #region 当控件尺寸改变时更改滚动条尺寸
        /// <summary> 思维导图尺寸改变时，滚动条尺寸也要跟着变
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mindMapNode_Resize(object sender, EventArgs e)
        {
            ResetMindMapPanelSize();
        }

        /// <summary> 当本控件的尺寸改变时，滚动条尺寸也要改变
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MindMap_Panel_Resize(object sender, EventArgs e)
        {
            //不知道什么原因，如果本控件尺寸改变后如果立即设置滚动条的尺寸，会出现Bug
            //例如外部Winform添加本控件后，Dock设为Fill，在窗体最大化，或从最大或变成正常态时将会出现Bug
            //所以当本控件尺寸发生改变时延迟200毫秒设置滚动条尺寸
            new Thread(() =>
            {
                Thread.Sleep(200);
                this.Invoke(new Action(() =>
                {
                    ResetMindMapPanelSize();
                }
                    ));
            }).Start();
        }

        /// <summary> 重新设置导图和容器在控件中的尺寸
        /// 
        /// </summary>
        private void ResetMindMapPanelSize()
        {
            Scroll_panel.Location = new Point(-this.HorizontalScroll.Value, -this.VerticalScroll.Value);

            int MaxHeight = this.Height * 2;//容器最大高度，父容器的2倍
            int MaxWidth = this.Width * 2;//容器最大宽度，父容器的2倍
            int MinHeight = mindMapNode.Height * 2;//容器最小高度，自身高度的两倍
            int MinWidth = mindMapNode.Width * 2;//容器最小宽度，自身宽度的两倍
            Scroll_panel.Height = MaxHeight > MinHeight ? MaxHeight : MinHeight;//优先最大高度
            Scroll_panel.Width = MaxWidth > MinWidth ? MaxWidth : MinWidth;//优先最大宽度

            #region 将容器滚动至居中位置

            int IntX = this.Scroll_panel.Width - this.Width;
            int IntY = this.Scroll_panel.Height - this.Height;
            Point PointTemp = new Point(IntX / 2, IntY / 2);
            this.AutoScrollPosition = PointTemp;
            #endregion 将容器滚动至居中位置

            #region 思维导图相对于容器居中
            int IntTemp = Scroll_panel.Height - mindMapNode.Height;
            IntTemp = IntTemp / 2;
            mindMapNode.Top = IntTemp;
            IntTemp = Scroll_panel.Width - mindMapNode.Width;
            IntTemp = IntTemp / 2;
            mindMapNode.Left = IntTemp;
            #endregion 思维导图相对于容器居中

            this.HorizontalScroll.Minimum = Scroll_panel.Width;
            this.VerticalScroll.Minimum = Scroll_panel.Height;
        }
        #endregion 当控件尺寸改变时更改滚动条尺寸

        private int FontSize = 13;
        /// <summary> 滚轮放大缩小
        /// 
        /// </summary>
        /// <param name="Send"></param>
        /// <param name="e"></param>
        private void OnMouseWhell(object Send, MouseEventArgs e)
        {
            
            if (Control.ModifierKeys == Keys.Control)
            {
                int ChangeValue = 1;//每次放大或缩小的数值
                if (e.Delta < 0) FontSize = FontSize - ChangeValue <= ChangeValue ? ChangeValue : FontSize - ChangeValue;
                else FontSize = FontSize + ChangeValue;

                Font TextFontTemp = new Font(new FontFamily("微软雅黑"), FontSize);
                this.Visible = false;
                
                this.Visible = true;
            }
        }
                     
    }
}