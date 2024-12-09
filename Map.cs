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
        private int currentPathStep = 0;

        public Map()
        {
            InitializeComponent();
        }

        private void Map_Load(object sender, EventArgs e)
        {
            pictureBox1.BackColor = Color.White;
            pictureBox1.Image = MainForm.mapOfField;

            timer1.Enabled = true;
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!isBtnChangeClicked)
            {
                pictureBox1.Image = MainForm.mapOfField;

                randomSearch = new RandomSearch(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);
                if (MainForm.randomSearch.Path != null)
                {
                    if (isStart)
                    {
                        if (MainForm.IsThereStartAndEndPoint(mapViewer))
                        {
                            // Рисуем часть пути до текущего шага
                            StartStepByStepDrawing(randomSearch.StartSearch(mapViewer, (float)mapViewer.RobotRadius), mapViewer, pictureBox1);

                            // Увеличиваем текущий шаг
                            currentPathStep++;
                            //pictureBox1.Refresh();
                        }
                    }
                }
            }
            else
            {
                pictureBox1.Image = MainForm.configImage;
            }

            //pictureBox1.Invalidate();
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
            else if (checkBox3.Checked)
            {
                MainForm.mapViewer.ClearPoints(x, y);
            }
            else
            {
                MainForm.mapViewer.SetWall(x, y);
            }

            //pictureBox1.Invalidate();
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

        private void button3_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = MainForm.mapOfField;

            isStart = true;
            timer2.Enabled = true;
            //StartStepByStepDrawing(randomSearch.Path, mapViewer, pictureBox1);
        }

        private int currentStepIndex = 0; // Индекс текущего шага

        //public void StartStepByStepDrawing(List<PointF> path, MapViewer mapViewer, PictureBox pictureBox)
        //{
        //    // Сохраняем исходное изображение карты, чтобы добавлять новые точки на уже существующую картину
        //    Bitmap updatedMap = new Bitmap(pictureBox.Image);

        //    // Удаляем предыдущий обработчик (если был добавлен)
        //    timer2.Tick -= Timer2_Tick;

        //    currentStepIndex = 0;

        //    // Добавляем новый обработчик события
        //    timer2.Tick += Timer2_Tick;

        //    void Timer2_Tick(object sender, EventArgs e)
        //    {
        //        if (currentStepIndex < path.Count)
        //        {
        //            // Отрисовываем текущую точку
        //            var currentPoint = path[currentStepIndex];

        //            // Передаем только нужную часть списка, используя индекс, без создания нового списка
        //            pictureBox1.Image = randomSearch.DrawSearchStep(path.ToArray(), mapViewer, currentStepIndex);
        //            //randomSearch.DrawPath(path, mapViewer);

        //            // Увеличиваем шаг
        //            currentStepIndex++;
        //        }
        //        else
        //        {
        //            timer2.Stop(); // Останавливаем таймер, когда отрисовка завершена
        //            timer2.Tick -= Timer2_Tick; // Удаляем обработчик
        //        }
        //    }

        //    timer2.Start(); // Запускаем таймер
        //}

        Bitmap updatedMap = new Bitmap(447, 278);
        Bitmap bmp = new Bitmap(MainForm.mapOfField);
        private bool isStart;

        public void StartStepByStepDrawing(List<PointF> path, MapViewer mapViewer, PictureBox pictureBox)
        {
            // Удаляем предыдущий обработчик (если был добавлен)
            timer2.Tick -= Timer2_Tick;

            currentStepIndex = 0;

            // Добавляем новый обработчик события
            timer2.Tick += Timer2_Tick;

            void Timer2_Tick(object sender, EventArgs e)
            {
                if (path != null)
                {
                    if (currentStepIndex < path.Count)
                    {
                        bmp = randomSearch.DrawSearchStep(path.ToArray(), mapViewer, currentStepIndex, randomSearch.deadEnds);

                        // Увеличиваем шаг
                        currentStepIndex++;
                    }
                    else
                    {
                        timer2.Stop(); // Останавливаем таймер
                        timer2.Tick -= Timer2_Tick; // Удаляем обработчик
                        var commands = randomSearch.TranslatePathToCommands(randomSearch.Path);

                        // Проверяем, что команда не пуста
                        if (commands == null || commands.Count == 0)
                        {
                            MessageBox.Show("Финальный путь не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Рисуем финальный путь
                        pictureBox1.Image = randomSearch.DrawFinalPath(randomSearch.Path, mapViewer);
                    }
                }
                // Обновляем отображение
                
            }
            pictureBox.Image = bmp;
            //pictureBox.Invalidate();

            timer2.Start(); // Запускаем таймер
        }


        private void timer2_Tick(object sender, EventArgs e)
        {
            //// Рисуем часть пути до текущего шага
            //StartStepByStepDrawing(MainForm.randomSearch.Path, mapViewer, pictureBox1);

            //// Увеличиваем текущий шаг
            //currentPathStep++;
            ////pictureBox1.Refresh();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            List<string> commands = randomSearch.TranslatePathToCommands(randomSearch.Path);

            randomSearch.SaveCommandsToCsv(commands);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var commands = randomSearch.TranslatePathToCommands(randomSearch.Path);

            // Проверяем, что команда не пуста
            if (commands == null || commands.Count == 0)
            {
                MessageBox.Show("Финальный путь не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Рисуем финальный путь
            //pictureBox1.Image = randomSearch.DrawFinalPath(commands, randomSearch.Path);
            //pictureBox1.Refresh();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
