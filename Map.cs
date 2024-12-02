using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Krasnyanskaya221327_Lab04_Sem5_Ver1.MainForm;

namespace Krasnyanskaya221327_Lab04_Sem5_Ver1
{
    public partial class Map : Form
    {
        bool isBtnChangeClicked = false;
        int countOfClicks = 0;

        public Map()
        {
            InitializeComponent();
        }

        private void Map_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            pictureBox1.BackColor = Color.White;
            if (!isBtnChangeClicked)
            {
                pictureBox1.Image = MainForm.mapOfField;
                if (MainForm.randomSearch.Trajectory != null)
                {
                    pictureBox1.Image = MainForm.randomSearch.Trajectory;
                }
            }
            else
            {
                pictureBox1.Image = MainForm.configImage;
            }
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            float zoomFactor = (float)pictureBox1.Image.Width / pictureBox1.ClientSize.Width;

            int x = (int)((e.X * zoomFactor) / MainForm.MapViewer.CellSize);
            int y = (int)((e.Y * zoomFactor) / MainForm.MapViewer.CellSize);

            if (checkBox1.Checked)
            {
                MainForm.mapViewer.SetStartPosition(x, y);
            }
            else if (checkBox2.Checked)
            {
                MainForm.mapViewer.SetEndPosition(x, y);
            }
            else
            {
                MainForm.mapViewer.ToggleCellState(x, y);
            }

            pictureBox1.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                Title = "Save map file"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                MainForm.mapViewer.SaveMap(saveFileDialog.FileName);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                Title = "Load map file"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                MainForm.mapViewer.LoadMap(openFileDialog.FileName);
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            isBtnChangeClicked = true;
            countOfClicks++;

            if (countOfClicks % 2 == 0)
            {
                isBtnChangeClicked = false;
            }
        }
    }
}
