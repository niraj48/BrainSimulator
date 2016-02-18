﻿using GoodAI.BrainSimulator.Forms;
using GoodAI.Core.Execution;
using GoodAI.Core.Observers;
using GoodAI.Core.Utils;
using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace GoodAI.School.GUI
{
    public partial class SchoolRunForm : DockContent
    {
        public List<LearningTaskNode> Data;
        public List<LevelNode> Levels;
        public List<List<AttributeNode>> Attributes;
        public PlanDesign Design;

        private List<DataGridView> LevelGrids;

        private readonly MainForm m_mainForm;
        private string m_runName;
        private SchoolWorld m_school;
        private ObserverForm m_observer;

        private int m_currentRow = -1;
        private int m_stepOffset = 0;
        private DateTime m_ltStart;

        private bool m_showObserver { get { return btnObserver.Checked; } }

        public string RunName
        {
            get { return m_runName; }
            set
            {
                m_runName = value;

                Text = String.IsNullOrEmpty(m_runName) ? "School run" : "School run - " + m_runName;
            }
        }

        private LearningTaskNode CurrentTask
        {
            get
            {
                return Data.ElementAt(m_currentRow);
            }
        }


        public SchoolRunForm(MainForm mainForm)
        {
            m_mainForm = mainForm;
            InitializeComponent();

            btnObserver.Checked = Properties.School.Default.ShowVisual;

            // here so it does not interfere with designer generated code
            btnRun.Click += new System.EventHandler(m_mainForm.runToolButton_Click);
            btnStop.Click += new System.EventHandler(m_mainForm.stopToolButton_Click);
            btnPause.Click += new System.EventHandler(m_mainForm.pauseToolButton_Click);
            btnStepOver.Click += new System.EventHandler(m_mainForm.stepOverToolButton_Click);
            btnDebug.Click += new System.EventHandler(m_mainForm.debugToolButton_Click);

            m_mainForm.SimulationHandler.StateChanged += UpdateButtons;
            m_mainForm.SimulationHandler.ProgressChanged += SimulationHandler_ProgressChanged;
            UpdateButtons(null, null);
        }

        private void SimulationHandler_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (m_school == null)
                return;

            ILearningTask runningTask = m_school.CurrentLearningTask;
            if (runningTask == null)
                return;

            if (m_currentRow < 0 || runningTask.GetType() != Data.ElementAt(m_currentRow).TaskType) // next LT
                GoToNextTask();

            if (runningTask.GetType() != Data.ElementAt(m_currentRow).TaskType) //should not happen at all - just a safeguard
            {
                MyLog.ERROR.WriteLine("One of the Learning Tasks was skipped. Stopping simulation.");
                return;
            }

            UpdateTaskData(runningTask);
        }

        private void UpdateButtons(object sender, MySimulationHandler.StateEventArgs e)
        {
            btnRun.Enabled = m_mainForm.runToolButton.Enabled;
            btnPause.Enabled = m_mainForm.pauseToolButton.Enabled;
            btnStop.Enabled = m_mainForm.stopToolButton.Enabled;
        }

        public void Ready()
        {
            UpdateGridData();
            PrepareSimulation();
            SetObserver();
            if (Properties.School.Default.AutorunEnabled && Data != null)
                btnRun.PerformClick();
        }

        public void UpdateGridData()
        {
            dataGridView1.DataSource = Data;
            dataGridView1.Invalidate();
        }

        private void UpdateTaskData(ILearningTask runningTask)
        {
            CurrentTask.Steps = (int)m_mainForm.SimulationHandler.SimulationStep - m_stepOffset;
            CurrentTask.Progress = (int)runningTask.Progress;
            TimeSpan diff = DateTime.UtcNow - m_ltStart;
            CurrentTask.Time = (float)Math.Round(diff.TotalSeconds, 2);

            UpdateGridData();
        }

        private void GoToNextTask()
        {
            m_currentRow++;
            m_stepOffset = (int)m_mainForm.SimulationHandler.SimulationStep;
            m_ltStart = DateTime.UtcNow; ;

            HighlightCurrentTask();
        }

        private void SetObserver()
        {
            if (m_showObserver)
            {
                if (m_observer == null)
                {
                    try
                    {
                        MyMemoryBlockObserver observer = new MyMemoryBlockObserver();
                        observer.Target = m_school.Visual;

                        if (observer == null)
                            throw new InvalidOperationException("No observer was initialized");

                        m_observer = new ObserverForm(m_mainForm, observer, m_school);

                        m_observer.TopLevel = false;
                        observerDockPanel.Controls.Add(m_observer);

                        m_observer.CloseButtonVisible = false;
                        m_observer.MaximizeBox = false;
                        m_observer.Size = observerDockPanel.Size + new System.Drawing.Size(16, 38);
                        m_observer.Location = new System.Drawing.Point(-8, -30);

                        m_observer.Show();
                    }
                    catch (Exception e)
                    {
                        MyLog.ERROR.WriteLine("Error creating observer: " + e.Message);
                    }
                }
                else
                {
                    m_observer.Show();
                    observerDockPanel.Show();
                }
            }
            else
            {
                if (m_observer != null)
                {
                    observerDockPanel.Hide();
                }
            }
        }

        private void SelectSchoolWorld()
        {
            m_mainForm.SelectWorldInWorldList(typeof(SchoolWorld));
            m_school = (SchoolWorld)m_mainForm.Project.World;
        }

        private void CreateCurriculum()
        {
            m_school.Curriculum = Design.AsSchoolCurriculum(m_school);
            // TODO: next two lines are probably not necessary
            foreach (ILearningTask task in m_school.Curriculum)
                task.SchoolWorld = m_school;
        }

        private void HighlightCurrentTask()
        {
            if (m_currentRow < 0)
                return;

            DataGridViewCellStyle defaultStyle = new DataGridViewCellStyle();
            DataGridViewCellStyle highlightStyle = new DataGridViewCellStyle();
            highlightStyle.BackColor = Color.PaleGreen;

            foreach (DataGridViewRow row in dataGridView1.Rows)
                foreach (DataGridViewCell cell in row.Cells)
                    if (row.Index == m_currentRow)
                        cell.Style = highlightStyle;
                    else
                        cell.Style = defaultStyle;
        }

        private void PrepareSimulation()
        {
            // data
            SelectSchoolWorld();
            CreateCurriculum();

            // gui
            m_stepOffset = 0;
            m_currentRow = -1;
            Data.ForEach(x => x.Steps = 0);
            Data.ForEach(x => x.Time = 0f);
            Data.ForEach(x => x.Progress = 0);
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            string columnName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (columnName.Equals(TaskType.Name) || columnName.Equals(WorldType.Name))
            {
                // I am not sure about how bad this approach is, but it get things done
                if (e.Value != null)
                {
                    Type typeValue = e.Value as Type;

                    DisplayNameAttribute displayNameAtt = typeValue.GetCustomAttributes(typeof(DisplayNameAttribute), true).FirstOrDefault() as DisplayNameAttribute;
                    if (displayNameAtt != null)
                        e.Value = displayNameAtt.DisplayName;
                    else
                        e.Value = typeValue.Name;
                }
            }
        }

        private void SchoolRunForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    {
                        btnRun.PerformClick();
                        break;
                    }
                case Keys.F7:
                    {
                        btnPause.PerformClick();
                        break;
                    }
                case Keys.F8:
                    {
                        btnStop.PerformClick();
                        break;
                    }
                case Keys.F10:
                    {
                        btnStepOver.PerformClick();
                        break;
                    }
            }
        }

        private void simulationStart(object sender, EventArgs e)
        {
            if (m_mainForm.SimulationHandler.State == MySimulationHandler.SimulationState.STOPPED)
                PrepareSimulation();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            DataGridView senderT = sender as DataGridView;
            int dataIndex;
            if (senderT.SelectedRows != null)
            {
                DataGridViewRow row = senderT.SelectedRows[0];
                dataIndex = row.Index;
            }
            else
            {
                dataIndex = 0;
            }
            LearningTaskNode ltNode = Data[dataIndex];
            Type ltType = ltNode.TaskType;
            Type ltWorld = ltNode.WorldType;
            ILearningTask lt = LearningTaskFactory.CreateLearningTask(ltType);
            
            TrainingSetHints hint = lt.TSProgression[0];

            Levels = new List<LevelNode>();
            LevelGrids = new List<DataGridView>();
            Attributes = new List<List<AttributeNode>>();
            tabControl1.TabPages.Clear();

            for (int i = 0; i < lt.TSProgression.Count; i++)
            {
                // create tab
                LevelNode ln = new LevelNode(i + 1);
                Levels.Add(ln);
                TabPage tp = new TabPage(ln.Text);
                tabControl1.TabPages.Add(tp);
                
                // create grid
                DataGridView dgv = new DataGridView();
                LevelGrids.Add(dgv);
                dgv.Parent = tp;
                dgv.Margin = new Padding(3);
                dgv.Dock = DockStyle.Fill;
                dgv.RowHeadersVisible = false;
                // create attributes
                Attributes.Add(new List<AttributeNode>());
                foreach (var attribute in lt.TSProgression[i])
                {
                    
                    AttributeNode an = new AttributeNode(
                        attribute.Key.Name,
                        attribute.Value,
                        attribute.Key.TypeOfValue);
                    Attributes.Last().Add(an);
                }

                dgv.DataSource = Attributes.Last();
                for (int k = 13; k > 2; k--)
                {
                    dgv.Columns.RemoveAt(k);
                }
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                tabControl1.Update();
            }
            
        }

        private void btnObserver_Click(object sender, EventArgs e)
        {
            Properties.School.Default.ShowVisual = (sender as ToolStripButton).Checked = !(sender as ToolStripButton).Checked;
            Properties.School.Default.Save();
            SetObserver();
        }
    }
}
