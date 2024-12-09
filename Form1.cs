using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.CompilerServices;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using System.Timers;

namespace Krasnyanskaya221327_Lab04_Sem5_Ver1
{
    public partial class MainForm : Form
    {
        public static UDPServer server;
        public static Bitmap bitmap;
        public static Bitmap MiniMap;
        public int threshold;

        static int count = 0;
        static int oldCount = 0;
        private int savedCount = 0;
        private int N = 0;

        private DateTime moveBackStartTime;
        private bool isMovingBack = false;
        private bool isMovingForward = false;
        private TimeSpan moveBackDuration = TimeSpan.FromSeconds(1);
        private int bumpCount = 0;

        public static Visualization visualization = new Visualization();
        public static Visualization vis = new Visualization();
        private bool isSend = false;
        public int countOfClick = 1;

        int previousForwardSpeed = 0;
        int previousBackwardSpeed = 0;

        public static MapViewer mapViewer = new MapViewer();
        public static Bitmap mapOfField = new Bitmap(447, 278);
        public static Bitmap configImage = new Bitmap(447, 278);

        public static Point startPoint, endPoint;

        public static RandomSearch randomSearch;

        public static int state = 0;
        public static int currentPathIndex = 0;
        public static DateTime lastStateChangeTime; // Для отслеживания времени с момента последнего состояния
        public static TimeSpan pauseDuration = TimeSpan.FromMilliseconds(1000); // Полсекунды паузы между действиями

        public static double RobotDirection = 1;

        public static float[] sensorAngles = { 0, 45, 90, 135, 180, -135, -90, -45 };
        public static double angleDiff = 0;

        public MainForm()
        {
            InitializeComponent();
        }

        public class UDPServer
        {
            public IPAddress IpAddress { get; set; }
            public int LocalPort { get; set; }
            public int RemotePort { get; set; }
            public UdpClient UdpClient { get; set; }
            public IPEndPoint IpEndPoint { get; set; }
            public byte[] Data { get; set; }
            public static Dictionary<string, int> DecodeText;

            public static string DecodeData { get; set; }
            public static int n, s, c, le, re, az, b, d0, d1, d2, d3, d4, d5, d6, d7, l0, l1, l2, l3, l4;

            public UDPServer(IPAddress ip, int localPort, int remotePort)
            {
                IpAddress = ip;
                LocalPort = localPort;
                RemotePort = remotePort;
                UdpClient = new UdpClient(LocalPort);
                IpEndPoint = new IPEndPoint(IpAddress, LocalPort);
            }

            public async Task ReceiveDataAsync()
            {
                while (true)
                {
                    var receivedResult = await UdpClient.ReceiveAsync();
                    Data = receivedResult.Buffer;
                    DecodingData(Data);
                }
            }

            public async Task SendDataAsync(byte[] data)
            {
                if (data != null)
                {
                    IPEndPoint pointServer = new IPEndPoint(IpAddress, RemotePort);
                    await UdpClient.SendAsync(data, data.Length, pointServer);
                }
            }

            public async Task SendRobotDataAsync()
            {
                string robotData = Robot.GetCommandsAsJson();
                byte[] dataToSend = Encoding.ASCII.GetBytes(robotData + "\n");
                await SendDataAsync(dataToSend);
            }

            private void DecodingData(byte[] data)
            {
                var message = Encoding.ASCII.GetString(data);
                DecodeText = JsonConvert.DeserializeObject<Dictionary<string, int>>(message);
                var lines = DecodeText.Select(kv => kv.Key + ": " + kv.Value.ToString());
                DecodeData = "IoT: " + string.Join(Environment.NewLine, lines);

                AnalyzeData(DecodeText);
            }

            private void AnalyzeData(Dictionary<string, int> pairs)
            {
                if (pairs.ContainsKey("n"))
                {
                    n = pairs["n"];
                    s = pairs["s"];
                    c = pairs["c"];
                    le = pairs["le"];
                    re = pairs["re"];
                    az = pairs["az"];
                    b = pairs["b"];
                    d0 = pairs["d0"];
                    d1 = pairs["d1"];
                    d2 = pairs["d2"];
                    d3 = pairs["d3"];
                    d4 = pairs["d4"];
                    d5 = pairs["d5"];
                    d6 = pairs["d6"];
                    d7 = pairs["d7"];
                    l0 = pairs["l0"];
                    l1 = pairs["l1"];
                    l2 = pairs["l2"];
                    l3 = pairs["l3"];
                    l4 = pairs["l4"];
                }
                else
                {
                    MessageBox.Show("No data");
                }
            }

        }

        public static class Robot
        {
            public static Dictionary<string, int> Commands = new Dictionary<string, int>
            {
                { "N", 0 },
                { "M", 0 },
                { "F", 0 },
                { "B", 0 },
                { "T", 0 },
            };

            public static bool isInStartZone = false;
            public static bool isInWaitingZone = false;
            public static bool isPalletGet = false;
            public static bool isReadyToPick = false;
            public static bool PickedOrder = false;
            public static bool ReturnedToFinal = false;
            public static bool FinalStateGot = false;
            public static int countOfOrders = 0;
            public static int n, s, c, le, re, az, b, d0, d1, d2, d3, d4, d5, d6, d7, l0, l1, l2, l3, l4;
            public static bool isFirstRotateDone = false, isFirstWayDone = false;

            public static Point robotPosition = new Point();
            private static double previousLeftEncoderCount;
            private static double previousRightEncoderCount;
            public static int index = 0;

            public static void UpdateData(Dictionary<string, int> pairs)
            {
                n = pairs["n"];
                s = pairs["s"];
                c = pairs["c"];
                le = pairs["le"];
                re = pairs["re"];
                az = pairs["az"];
                b = pairs["b"];
                d0 = pairs["d0"];
                d1 = pairs["d1"];
                d2 = pairs["d2"];
                d3 = pairs["d3"];
                d4 = pairs["d4"];
                d5 = pairs["d5"];
                d6 = pairs["d6"];
                d7 = pairs["d7"];
                l0 = pairs["l0"];
                l1 = pairs["l1"];
                l2 = pairs["l2"];
                l3 = pairs["l3"];
                l4 = pairs["l4"];
            }



            public static string GetCommandsAsJson()
            {
                return JsonConvert.SerializeObject(Commands);
            }

            public static void LoadCommandsFromJson(string json)
            {
                var newCommands = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                if (newCommands != null)
                {
                    Commands = newCommands;
                }
            }

            public static void SetCommand(string key, int value)
            {
                if (Commands.ContainsKey(key))
                {
                    Commands[key] = value;
                }
                else
                {
                    throw new ArgumentException("Команда с таким ключом не существует.");
                }
            }

            public static void UpdateDecodeText()
            {
                UDPServer.DecodeText["n"] = Commands["N"];
            }

            public static void SendOldCommands()
            {
                string oldCommands = JsonConvert.SerializeObject(UDPServer.DecodeText, Newtonsoft.Json.Formatting.None);

                byte[] data = Encoding.ASCII.GetBytes(oldCommands + "\n");

                UdpClient udpCommands = new UdpClient();
                IPEndPoint pointServer = new IPEndPoint(server.IpAddress, server.RemotePort);
                udpCommands.Send(data, data.Length, pointServer);

                string jsonString = JsonConvert.SerializeObject(Commands, Newtonsoft.Json.Formatting.None);
                byte[] dataForRobot = Encoding.ASCII.GetBytes(jsonString + "\n");

                udpCommands.Send(dataForRobot, dataForRobot.Length, pointServer);
            }

            public static void RotateRight()
            {
                SetCommand("B", 25);
            }

            public static void RotateLeft()
            {
                SetCommand("B", -25);
            }

            public static void MoveStraight()
            {
                SetCommand("F", 100);
            }

            public static void MoveBack()
            {
                SetCommand("F", -100);
            }

            public static void Stop()
            {
                SetCommand("B", 0);
                SetCommand("F", 0);
            }

            public static void MoveBackWhenBump()
            {
                SetCommand("F", -70);
                SetCommand("B", -25);
            }

            public static Point CalculateCurrentPoint(int[] distances, float[] sensorAngles)
            {
                Point currentPosition = new Point();
                float totalDeltaX = 0;
                float totalDeltaY = 0;

                // Для каждого дальномера
                for (int i = 0; i < distances.Length; i++)
                {
                    // Получаем угол датчика в радианах
                    float angleInRadians = sensorAngles[i] * (float)Math.PI / 180;

                    // Получаем расстояние от текущего дальномера
                    float distance = distances[i];

                    // Вычисляем вклад этого дальномера в смещение по осям
                    float deltaX = distance * (float)Math.Cos(angleInRadians);
                    float deltaY = distance * (float)Math.Sin(angleInRadians);

                    // Суммируем смещения всех датчиков
                    totalDeltaX += deltaX;
                    totalDeltaY += deltaY;
                }

                // Рассчитываем среднее смещение по X и Y
                currentPosition.X += (int)(totalDeltaX / distances.Length);  // делим на количество датчиков для усреднения
                currentPosition.Y += (int)(totalDeltaY / distances.Length);  // делим на количество датчиков для усреднения

                return currentPosition;
            }

            public static void MoveAlongPath(List<string> commands, Visualization vis)
            {
                const double tolerance = 5.0; // Допустимая погрешность для достижения цели
                PointF current = vis.RobotPosition;

                if (Robot.index < commands.Count)
                {
                    // Разбираем текущую команду
                    string[] parts = commands[Robot.index].Split(';');
                    int direction = int.Parse(parts[0]);
                    float targetX = float.Parse(parts[1]);
                    float targetY = float.Parse(parts[2]);

                    PointF target = new PointF(targetX, targetY);

                    // Вычисляем расстояние до цели
                    double distanceX = target.X - current.X;
                    double distanceY = target.Y - current.Y;
                    double distanceToTarget = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);

                    // Проверяем, достиг ли робот цели с учетом погрешности
                    if (distanceToTarget <= tolerance)
                    {
                        Robot.index++; // Переходим к следующей команде
                        if (Robot.index >= commands.Count)
                        {
                            // Если команды закончились, останавливаем робот
                            SetCommand("B", 0); // Остановить поворот
                            SetCommand("F", 0); // Остановить движение
                            
                        }
                    }
                    else
                    {
                        // Выполняем движение на основе текущего направления
                        ExecuteDirectionCommand(direction);
                    }
                }
            }


            private static void ExecuteDirectionCommand(int direction)
            {
                switch (direction)
                {
                    case 0: // Стоп
                        SetCommand("B", 0);
                        SetCommand("F", 0);
                        break;
                    case 1: // Север
                        SetCommand("B", 0);
                        SetCommand("F", 50); // Двигаемся вперёд
                        break;
                    case 2: // Восток
                        SetCommand("B", -25); // Поворот по часовой
                        SetCommand("F", 50); // Движение вперёд
                        break;
                    case 3: // Юг
                        SetCommand("B", 0);
                        SetCommand("F", 50); // Двигаемся назад
                        break;
                    case 4: // Запад
                        SetCommand("B", 25); // Поворот против часовой
                        SetCommand("F", 50); // Движение вперёд
                        break;
                }
            }


            //public static void MoveAlongPath(List<PointF> path, MapViewer mapViewer, int[] distances)
            //{
            //    if (UDPServer.b == 1)
            //    {
            //        SetCommand("B", -50);
            //        SetCommand("F", 0);
            //    }
            //    else
            //    {
            //        // Получаем текущую и следующую точку на пути
            //        double scaleX = path[index].X;
            //        double scaleY = path[index].Y;
            //        PointF next = new PointF((int)scaleX, (int)scaleY);

            //        // Получаем текущую позицию робота
            //        PointF current = new PointF(vis.RobotPosition.X, vis.RobotPosition.Y);

            //        // Двигаемся к точке, проверяя, что робот может двигаться
            //        MovingToPoint(path, vis, distances, sensorAngles);
            //    }
            //}

            //private static bool isTurning = false;

            //public static void MovingToPoint(List<PointF> path, Visualization vis, int[] sensors, float[] angles)
            //{
            //    const double tolerance = 0.5; // Допустимая погрешность

            //    if (Robot.index < path.Count)
            //    {
            //        PointF target = path[Robot.index];
            //        PointF current = vis.RobotPosition;

            //        // Вычисляем расстояние до цели
            //        double distanceX = target.X - current.X;
            //        double distanceY = target.Y - current.Y;

            //        // Проверяем, достиг ли робот цели
            //        if (Math.Abs(distanceX) <= tolerance && Math.Abs(distanceY) <= tolerance)
            //        {
            //            Robot.index++; // Переходим к следующей точке
            //            isTurning = true; // Готовимся к следующему повороту
            //            return;
            //        }

            //        // Угол до цели
            //        double angleToTarget = Math.Atan2(distanceY, distanceX) * (180 / Math.PI);

            //        // Текущий угол робота
            //        double currentAngle = vis.CalcPosition(UDPServer.le, UDPServer.re).Item2 * (180 / Math.PI);

            //        // Разница углов, нормализованная в диапазоне [-180, 180]
            //        double angleDifference = NormalizeAngle(angleToTarget - currentAngle);
            //        angleDiff = angleDifference;

            //        // Если требуется поворот
            //        if (Math.Abs(angleDifference) > 5 && !isTurning)
            //        {
            //            if (angleDifference > 0)
            //            {
            //                SetCommand("B", 25); // Поворот по часовой стрелке
            //                SetCommand("F", 0);  // Без движения вперед
            //            }
            //            else
            //            {
            //                SetCommand("B", -25); // Поворот против часовой стрелки
            //                SetCommand("F", 0);
            //            }
            //            lastStateChangeTime = DateTime.Now; // Сохраняем время последней команды
            //            isTurning = true;
            //        }
            //        else
            //        {
            //            // Если поворот завершен, начинаем движение вперед
            //            if (isTurning)
            //            {
            //                SetCommand("B", 0); // Останавливаем поворот
            //                SetCommand("F", 50); // Двигаемся вперед
            //                isTurning = false; // Поворот завершен
            //            }
            //        }
            //    }
            //}


            //public static void MovingToPoint(List<PointF> path, Visualization vis, int[] sensors, float[] angles)
            //{
            //    const double tolerance = 1; // Допустимая погрешность

            //    if (Robot.index < path.Count)
            //    {
            //        PointF target = path[Robot.index];
            //        PointF current = vis.RobotPosition;

            //        // Вычисляем расстояние до цели
            //        double distanceX = target.X - current.X;
            //        double distanceY = target.Y - current.Y;

            //        // Проверяем, достиг ли робот цели
            //        if (Math.Abs(distanceX) <= tolerance && Math.Abs(distanceY) <= tolerance)
            //        {
            //            Robot.index++; // Переходим к следующей точке
            //            return;
            //        }

            //        // Угол до цели
            //        double angleToTarget = Math.Atan2(distanceY, distanceX) * (180 / Math.PI);

            //        // Текущий угол робота
            //        double currentAngle = vis.CalcPosition(UDPServer.re, UDPServer.le).Item2 * (180 / Math.PI);

            //        // Разница углов, нормализованная в диапазоне [-180, 180]
            //        double angleDifference = NormalizeAngle(angleToTarget - currentAngle);

            //        //// Если разница углов значительна, поворачиваем робот
            //        //if (Math.Abs(angleDifference) > 10) // Условие для точности поворота
            //        //{
            //        //    //if (angleDifference > 0)
            //        //    //{
            //        //    //    SetCommand("B", 25);
            //        //    //    SetCommand("F", 0);
            //        //    //}
            //        //    //else
            //        //    //{
            //        //    //    SetCommand("B", -25);
            //        //    //    SetCommand("F", 0);
            //        //    //}
            //        //}
            //        //else
            //        //{
            //        //    // Если робот направлен на цель, двигаемся вперёд
            //        //    SetCommand("F", 50);
            //        //    SetCommand("B", 0);
            //        //}

            //        //if (angleDifference > 0)
            //        //{
            //        //    lastStateChangeTime = DateTime.Now;
            //        //    SetCommand("B", 25);
            //        //    SetCommand("F", 50);

            //        //}
            //        //else if (angleDifference < 0)
            //        //{
            //        //    lastStateChangeTime = DateTime.Now;
            //        //    SetCommand("B", -25);
            //        //    SetCommand("F", 50);

            //        //}
            //        //else if (Math.Abs(angleDifference) < 10 || (DateTime.Now - lastStateChangeTime).TotalMilliseconds >= 1000)
            //        //{
            //        //    SetCommand("B", 0);
            //        //    SetCommand("F", 50);
            //        //}

            //        if (angleDifference > 0)
            //        {
            //            lastStateChangeTime = DateTime.Now;
            //            SetCommand("B", 25);
            //            SetCommand("F", 50);

            //        }
            //        else if (angleDifference < 0)
            //        {
            //            lastStateChangeTime = DateTime.Now;
            //            SetCommand("B", -25);
            //            SetCommand("F", 50);

            //        }
            //        SetCommand("B", 0);
            //        SetCommand("F", 50);
            //    }
            //}

            public static List<Point> GetSafeNeighbors(Point current, MapViewer mapViewer)
            {
                List<Point> safeNeighbors = new List<Point>();
                int[] directions = { -1, 0, 1 };  // Направления по X и Y (вверх, вниз, влево, вправо)

                foreach (int dx in directions)
                {
                    foreach (int dy in directions)
                    {
                        if (dx == 0 && dy == 0) continue;  // Пропускаем текущую позицию

                        int nx = current.X + dx;
                        int ny = current.Y + dy;

                        if (mapViewer.CanFitRobot(nx, ny))  // Проверяем доступность клетки
                        {
                            safeNeighbors.Add(new Point(nx, ny));
                        }
                    }
                }
                return safeNeighbors;
            }

            public static List<Point> FindAlternativePath(Point start, Point end, MapViewer mapViewer)
            {
                List<Point> path = new List<Point>();
                // Пример: добавляем start и end как временный путь
                path.Add(start);
                path.Add(end);
                return path;
            }

            public static void MoveToNextPoint(PointF currentPos, PointF targetPos, PointF prev, int[] distances, MapViewer mapViewer)
            {
                Point calculatedPos = CalculateCurrentPoint(distances, sensorAngles);

                // Преобразуем вычисленные координаты в индексы карты
                int gridX = (int)(currentPos.X / MapViewer.CellSize);
                int gridY = (int)(currentPos.Y / MapViewer.CellSize);

                // Проверим, находится ли вычисленная позиция внутри карты
                if (gridX >= 0 && gridX < MapViewer.GridWidth && gridY >= 0 && gridY < MapViewer.GridHeight)
                {
                    // Проверяем, можно ли роботу встать в эту клетку
                    if (mapViewer.CanFitRobot(gridX, gridY))
                    {
                        // Если клетка доступна, двигаться к следующей точке пути
                        // Двигаем робота в сторону следующей точки пути
                        MoveTowardsTargetAsync(currentPos, targetPos, prev);
                    }
                }
            }

            public static void MoveTowardsTargetAsync(PointF currentPos, PointF targetPos, PointF prev)
            {
                float deltaX = targetPos.X - currentPos.X;
                float deltaY = targetPos.Y - currentPos.Y;

                // Вычисляем угол направления
                double angleToTarget = Math.Atan2(deltaY, deltaX) * (180 / Math.PI);
                double robotAngle = Math.Atan2(currentPos.Y, currentPos.X) * (180 / Math.PI);

                if (currentPos == prev)
                {
                    // Движение вперед с фиксированной скоростью
                    SetCommand("F", 50);
                }
                else
                {
                    // Поворот робота к цели
                    RotateToAngle(currentPos, targetPos, prev);

                    // Движение вперед с фиксированной скоростью
                    SetCommand("F", 50);
                    //SetCommand("B", 0);
                }
            }

            public static int delayTicks = 0;  // Счетчик тиков для задержки
            private static bool isDelaying = false;  // Флаг, указывающий, идет ли задержка

            public static double CalculateAngle(PointF current, PointF target)
            {
                double deltaX = target.X - current.X;
                double deltaY = target.Y - current.Y;
                return Math.Atan2(deltaY, deltaX) * 180 / Math.PI;  // Возвращает угол в градусах
            }

            public static float CalculateTurnPoint(PointF targetPosition, PointF prev)
            {
                // Получаем координаты робота и целевой точки
                float currentX = visualization.RobotPosition.X;
                float currentY = visualization.RobotPosition.Y;
                float targetX = targetPosition.X;
                float targetY = targetPosition.Y;

                // Вычисляем угол к целевой точке в радианах
                float targetAngleRad = (float)Math.Atan2(targetY - currentY, targetX - currentX);

                double robotDirection = CalcRobotDirection(new PointF(currentX, currentY), prev);

                // Преобразуем текущий угол направления робота в радианы
                double currentDirectionRad = robotDirection * (float)Math.PI / 180;

                // Вычисляем разницу между текущим направлением и направлением к цели
                float deltaAngleRad = targetAngleRad - (float)currentDirectionRad;

                // Приводим угол к диапазону от -π до π, чтобы угол был минимальным
                deltaAngleRad = (float)((deltaAngleRad + Math.PI) % (2 * Math.PI) - Math.PI);

                // Преобразуем разницу в градусы
                float deltaAngle = deltaAngleRad * (180 / (float)Math.PI);

                // Обновляем направление робота с учетом этого угла поворота
                robotDirection = (robotDirection + deltaAngle) % 360;

                return deltaAngle;
            }

            public static void RotateToAngle(PointF current, PointF target, PointF prev)
            {
                double angleToTarget = CalculateTurnAngleToPoint(current, target);
                double currentAngle = CalcRobotDirection(current, prev);

                // Разница углов с нормализацией
                double angleDiff = NormalizeAngle(angleToTarget - currentAngle);

                // Команды поворота
                double turnSpeed = Math.Min(Math.Abs(angleDiff), 25);  // Ограничение максимальной скорости
                turnSpeed = Math.Max(turnSpeed, 10);  // Минимальная скорость

                if (angleDiff > 0)
                {
                    SetCommand("B", -(int)turnSpeed);  // Поворот вправо
                }
                else if (angleDiff < 0)
                {
                    SetCommand("B", (int)turnSpeed);  // Поворот влево
                }
            }


            public static double CalcRobotDirection(PointF current, PointF prev)
            {
                double deltaX = current.X - prev.X;
                double deltaY = current.Y - prev.Y;
                double angle = Math.Atan2(deltaY, deltaX); // угол относительно оси X в радианах

                // Преобразуем угол в градусы
                double angleDegrees = angle * (180.0 / Math.PI);

                // Возвращаем угол в градусах
                return angleDegrees;
            }

            //public static Point CalculateDistance(int[] distances)
            //{
            //    int[] frontDistances = { distances[7], distances[0], distances[1] };
            //    int[] rearDistances = { distances[3], distances[4], distances[5] };

            //    // Вычисляем среднее расстояние для переднего и заднего датчиков
            //    float averageFrontDistance = (float)frontDistances.Average();
            //    float averageRearDistance = (float)rearDistances.Average();

            //    // Примерный угол направления, если разница в расстояниях переднего и заднего датчиков значительна
            //    float directionInRadians = 0;

            //    if (averageFrontDistance != averageRearDistance)
            //    {
            //        // Примерный угол, который мы получаем из разницы в расстояниях (упрощенная модель)
            //        directionInRadians = (averageFrontDistance - averageRearDistance) * 0.1f; // Это коэффициент, его можно настроить
            //    }

            //    // Усредненное расстояние всех дальномеров (если их несколько)
            //    float averageDistance = (float)((frontDistances.Average() + rearDistances.Average()) / 2);

            //    // Вычисление смещения робота по осям с учетом направления
            //    float deltaX = averageDistance * (float)Math.Cos(directionInRadians);
            //    float deltaY = averageDistance * (float)Math.Sin(directionInRadians);

            //    // Обновляем позицию робота
            //    RobotPosition.X += (int)deltaX;
            //    RobotPosition.Y += (int)deltaY;

            //    return RobotPosition;
            //}
        }

        public class Visualization
        {
            public Graphics Graphics;
            public Bitmap MiniMap = new Bitmap(410, 280);
            public PointF RobotPosition = new PointF(); // Позиция робота на экране (фиксированная)
            public float RobotDirection = 1;

            private List<PointF> Trajectory = new List<PointF>();
            private List<PointF> Obstacles = new List<PointF>();
            private List<PointF> ObstaclesState = new List<PointF>();
            private List<PointF> PreviousObstacles = new List<PointF>();
            private List<PointF> Walls = new List<PointF>(); // Список для стен
            private List<PointF> ImgWalls = new List<PointF>();

            private int previousLeftEncoder = 0; // Предыдущее значение энкодера левого колеса
            private int previousRightEncoder = 0; // Предыдущее значение энкодера правого колеса

            private PointF FixedRobotPosition = new PointF(205, 140); // Фиксированная точка робота на экране

            public void CalculateDistance(float[] distances)
            {
                // Обновляем текущее направление робота (предполагается, что RobotDirection обновляется при каждом повороте)
                float directionInRadians = RobotDirection * (float)Math.PI / 180;

                // Здесь используем среднее значение расстояний с дальномеров
                float averageDistance = distances.Average();

                // Вычисляем смещение робота по осям с учетом направления
                float deltaX = averageDistance * (float)Math.Cos(directionInRadians);
                float deltaY = averageDistance * (float)Math.Sin(directionInRadians);

                // Обновляем позицию робота
                RobotPosition.X += deltaX;
                RobotPosition.Y += deltaY;

                if (Robot.Commands["F"] == 0)
                {
                    TurnTrajectory(-Robot.Commands["B"], count);
                }
                else
                {
                    Trajectory.Add(new PointF(RobotPosition.X, RobotPosition.Y));
                }
            }

            public static float scale = 2;

            public Bitmap DrawMiniMap()
            {
                // Создаем графический контекст для мини-карты
                Graphics g = Graphics.FromImage(MiniMap);
                g.Clear(Color.Black); // Очищаем мини-карту

                // Центр карты
                float centerX = MiniMap.Width / 2;
                float centerY = MiniMap.Height / 2;
                g.TranslateTransform(centerX, centerY);

                UpdatePosition(UDPServer.re, UDPServer.le, true);

                // Рисуем стены как линии, соединяющие точки
                

                // Рисуем препятствия
                foreach (var obstacle in Obstacles)
                {
                    PointF adjustedObstacle = new PointF(obstacle.X - RobotPosition.X, obstacle.Y - RobotPosition.Y);
                    g.FillRectangle(Brushes.Red, adjustedObstacle.X * scale - 3, adjustedObstacle.Y * scale - 3, 6, 6);
                }

                // Отрисовываем траекторию с увеличенным масштабом
                // Отрисовываем все точки траектории
                foreach (var point in Trajectory)
                {
                    PointF miniPoint = new PointF(point.X - RobotPosition.X, point.Y - RobotPosition.Y);
                    g.FillEllipse(Brushes.Blue, miniPoint.X * 30 * scale - 3, miniPoint.Y * 30 * scale - 3, 4, 4);
                }

                // Текущее положение робота
                g.FillEllipse(Brushes.Purple, 0 - 5, 0 - 5, 10, 10); // Центрированное отображение робота

                return MiniMap;
            }



            // Функция для масштабирования точки без учета смещения
            private PointF ScalePoint(PointF point, float scale)
            {
                float scaledX = point.X * scale; // Масштабируем координату X
                float scaledY = point.Y * scale; // Масштабируем координату Y
                return new PointF(scaledX, scaledY);
            }

            private void UpdateWalls(float threshold)
            {
                ImgWalls.Clear(); // Очищаем старые стены для рисования на мини-карте

                // Усредняем координаты между предыдущими и текущими препятствиями
                for (int i = 0; i < Obstacles.Count; i++)
                {
                    PointF previous = (i < PreviousObstacles.Count) ? PreviousObstacles[i] : Obstacles[i]; // Если нет предыдущих данных, используем текущие
                    PointF current = Obstacles[i];

                    // Проверяем, не превышает ли расстояние между текущими и предыдущими координатами порог
                    float distance = (float)Math.Sqrt(Math.Pow(current.X - previous.X, 2) + Math.Pow(current.Y - previous.Y, 2));

                    if (distance <= threshold)
                    {
                        // Усредняем координаты
                        PointF wall = new PointF(
                            (previous.X + current.X) / 2,
                            (previous.Y + current.Y) / 2
                        );

                        ImgWalls.Add(wall); // Добавляем усредненные стены для рисования на мини-карте
                        Walls.Add(wall); // Также добавляем в Walls для заливки
                    }
                }

                // Обновляем список предыдущих препятствий для следующего вызова
                PreviousObstacles = new List<PointF>(Obstacles);
            }


            // Метод для перевода координат в масштаб мини-карты
            private PointF ScalePoint(PointF point, float scale, Rectangle miniMapRect)
            {
                return new PointF(
                    miniMapRect.X + (point.X * scale),
                    miniMapRect.Y + (point.Y * scale)
                );
            }

            public bool isTurning = false;

            // Метод DrawRobot с фиксацией траектории
            public Bitmap DrawRobot(int robotAngle)
            {
                Bitmap view = new Bitmap(410, 280);

                using (Graphics g = Graphics.FromImage(view))
                {
                    g.Clear(Color.Black);

                    // Расчет смещения относительно фиксированной позиции робота
                    PointF translation = new PointF(FixedRobotPosition.X - RobotPosition.X, FixedRobotPosition.Y - RobotPosition.Y);

                    if (Walls.Count > 0)
                    {
                        FillSpaceToWalls(g, translation);
                    }

                    // Рисуем препятствия
                    foreach (var point in Obstacles)
                    {
                        PointF translatedPoint = TranslatePoint(point, translation);
                        g.FillRectangle(Brushes.Red, translatedPoint.X - 3, translatedPoint.Y - 3, 6, 6);
                    }

                    UpdatePosition(UDPServer.le, UDPServer.re, true);
                    g.FillEllipse(Brushes.Purple, FixedRobotPosition.X - 5, FixedRobotPosition.Y - 5, 10, 10);
                }

                return view;
            }

            private void FillSpaceToWalls(Graphics g, PointF translation)
            {
                if (Walls != null && Walls.Count > 0)
                {
                    // Получаем углы, под которыми находятся препятствия
                    List<float> angles = new List<float> { 0, 45, 90, 135, 180, -45, -90, -135 };

                    // Проходим по каждому препятствию
                    for (int i = 0; i < Walls.Count; i++)
                    {
                        PointF currentWall = TranslatePoint(Walls[i], translation);

                        // Угол, под которым находится текущее препятствие
                        float angle = angles[i % angles.Count]; // Используем модуль для повторения углов

                        // Определяем следующую стену для создания сектора
                        PointF nextWall;
                        if (i + 1 < Walls.Count)
                        {
                            nextWall = TranslatePoint(Walls[i + 1], translation);
                        }
                        else
                        {
                            // Если это последнее препятствие, замыкаем с первым
                            nextWall = TranslatePoint(Walls[0], translation);
                        }

                        // Создаем точки для заполнения пространства (треугольник)
                        PointF[] points = new PointF[]
                        {
                FixedRobotPosition, // Центр робота
                currentWall,        // Текущая стена
                nextWall            // Следующая стена
                        };

                        // Рисуем заполнение треугольника
                        g.FillPolygon(Brushes.LightBlue, points);
                    }
                }
            }

            private bool IsAtStartingPosition(float threshold = 5.0f)
            {
                // Вычисляем расстояние от текущей позиции робота до начальной позиции
                float distance = (float)Math.Sqrt(Math.Pow(RobotPosition.X - FixedRobotPosition.X, 2) +
                                                  Math.Pow(RobotPosition.Y - FixedRobotPosition.Y, 2));

                // Если расстояние меньше порога, возвращаем true
                return distance < threshold;
            }

            // Метод для перевода точек относительно неподвижной позиции робота
            private PointF TranslatePoint(PointF point, PointF translation)
            {
                return new PointF(point.X + translation.X, point.Y + translation.Y);
            }

            private void DrawWalls(Graphics g, PointF translation)
            {
                foreach (var wall in Walls)
                {
                    PointF translatedWall = TranslatePoint(wall, translation);
                    g.FillRectangle(Brushes.Red, translatedWall.X - 3, translatedWall.Y - 3, 6, 6); // Рисуем стены красным цветом
                }
            }

            public void UpdatePosition(int le, int re, bool updateTrajectory = true)
            {
                const float wheelBase = 0.4f;
                const float wheelDiameter = 0.1f;
                const float distancePerTick = (float)(Math.PI * wheelDiameter / 360);

                float distanceLeft = (le - previousLeftEncoder) * distancePerTick;
                float distanceRight = (re - previousRightEncoder) * distancePerTick;

                if (distanceLeft == 0 && distanceRight == 0) return;

                float distance = (distanceLeft + distanceRight) / 2;
                float angleChange = (distanceRight - distanceLeft) / wheelBase;

                RobotDirection = (RobotDirection + angleChange * (180 / (float)Math.PI)) % 360;
                float directionInRadians = RobotDirection * (float)Math.PI / 180;

                float deltaX = distance * (float)Math.Cos(directionInRadians);
                float deltaY = distance * (float)Math.Sin(directionInRadians);

                RobotPosition.X += deltaX;
                RobotPosition.Y += deltaY;

                if (updateTrajectory)
                {
                    Trajectory.Add(new PointF(RobotPosition.X, RobotPosition.Y));
                }

                previousLeftEncoder = le;
                previousRightEncoder = re;
            }

            public (PointF, double) CalcPosition(int le, int re, bool updateTrajectory = true)
            {
                const float wheelBase = 0.4f;
                const float wheelDiameter = 0.1f;
                const float distancePerTick = (float)(Math.PI * wheelDiameter / 360);

                float distanceLeft = (le - previousLeftEncoder) * distancePerTick;
                float distanceRight = (re - previousRightEncoder) * distancePerTick;

                if (distanceLeft == 0 && distanceRight == 0) return (new PointF(RobotPosition.X, RobotPosition.Y), 0);

                float distance = (distanceLeft + distanceRight) / 2;
                float angleChange = (distanceRight - distanceLeft) / wheelBase;

                RobotDirection = (RobotDirection + angleChange * (180 / (float)Math.PI)) % 360;
                float directionInRadians = RobotDirection/* * (float)Math.PI / 180*/;

                float deltaX = distance * (float)Math.Cos(directionInRadians);
                float deltaY = distance * (float)Math.Sin(directionInRadians);

                RobotPosition.X += deltaX;
                RobotPosition.Y += -deltaY;

                previousLeftEncoder = le;
                previousRightEncoder = re;

                return (new PointF(RobotPosition.X, RobotPosition.Y), directionInRadians);
            }

            private bool isMovingForwardAfterTurn = false; // Флаг для отслеживания движения вперед после поворота
            private int moveForwardDelay = 1000; // Задержка в миллисекундах

            public void TurnTrajectory(float angularSpeed, float turnTime)
            {
                // Преобразуем угловую скорость из градусов в радианы
                float angularSpeedRad = angularSpeed * (float)Math.PI / 180;
                // Вычисляем угол поворота
                float deltaAngle = angularSpeedRad * turnTime;

                // Обновляем направление робота в градусах
                RobotDirection = (RobotDirection + deltaAngle * (180 / (float)Math.PI)) % 360;

                // Если угловая скорость отрицательна, поворот влево, положительная — вправо
                float turnRadius = 1.0f; // Допустим, радиус поворота робота, можно менять при необходимости
                float dx = turnRadius * (float)Math.Sin(deltaAngle);
                float dy = turnRadius * (1 - (float)Math.Cos(deltaAngle));

                // Учитываем направление робота
                float directionRad = RobotDirection * (float)Math.PI / 180;

                // Вычисляем новую позицию робота с учетом текущего направления
                float newX = RobotPosition.X + (float)(Math.Cos(directionRad) * dx - Math.Sin(directionRad) * dy);
                float newY = RobotPosition.Y + (float)(Math.Sin(directionRad) * dx + Math.Cos(directionRad) * dy);

                // Обновляем текущую позицию робота
                RobotPosition = new PointF(newX, newY);

                // Добавляем текущую позицию в траекторию
                Trajectory.Add(new PointF(newX, newY));
            }


            public void DetectObstaclesFromSensors(int threshold, params int[] distances)
            {
                Obstacles.Clear();
                ImgWalls.Clear();
                Walls.Clear();

                // Углы, соответствующие каждому дальномеру
                float[] angles = { 0, 45, 90, 135, 180, -135, -90, -45 };

                for (int i = 0; i < distances.Length; i++)
                {
                    if (distances[i] < threshold)
                    {
                        // Получаем позицию препятствия по данным дальномера и углу
                        PointF obstacle = GetObstaclePosition(distances[i], RobotDirection + angles[i]);

                        if (!Obstacles.Contains(obstacle))
                        {
                            Obstacles.Add(obstacle);
                            ObstaclesState.Add(obstacle);
                            ImgWalls.Add(obstacle);
                            Walls.Add(obstacle);
                        }
                    }
                }

                UpdateWalls(10.0f);
            }

            private PointF GetObstaclePosition(float distance, float angle)
            {
                float angleInRadians = angle * (float)Math.PI / 180;
                float x = RobotPosition.X + distance * (float)Math.Cos(angleInRadians);
                float y = RobotPosition.Y + distance * (float)Math.Sin(angleInRadians);
                return new PointF(x, y);
            }
        }

        public class MapViewer
        {
            public float[,] MapData;
            public const int GridWidth = 64;
            public const int GridHeight = 40;
            public double RobotRadius = 0.1;  // Радиус робота в метрах
            public const float MiniCellSize = 0.6f;    // Размер клетки в метрах
            public static int CellSize = (int)(25 * 0.4);    // Размер клетки в пикселях на экране

            public Bitmap Map;
            private Bitmap ConfigSpace;

            private Point? startPosition = null;
            private Point? endPosition = null;
            private List<Point> startPositions = new List<Point>();
            private List<Point> endPositions = new List<Point>();

            public void SetStartPosition(int x, int y)
            {
                startPositions.Add(new Point(x, y));
                MapData[y, x] = 2;
                mapOfField = DrawMap();
            }

            public void SetWall(int x, int y)
            {
                MapData[y, x] = 1;
                mapOfField = DrawMap();
            }
            public void SetEndPosition(int x, int y)
            {
                endPositions.Add(new Point(x, y));
                MapData[y, x] = 3;
                mapOfField = DrawMap();
            }

            public void ClearPoints(int x, int y)
            {
                if (MapData[y, x] == 2)
                {
                    startPositions.Remove(new Point(x, y));
                    MapData[y, x] = 0;
                }
                else if (MapData[y, x] == 3)
                {
                    endPositions.Remove(new Point(x, y));
                    MapData[y, x] = 0;
                }
                else
                {
                    MapData[y, x] = 0;
                }
                
                mapOfField = DrawMap();
            }

            public Bitmap OpenMap()
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt",
                    Title = "Select a map file"
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadMap(openFileDialog.FileName);
                    return Map;
                }

                return Map;
            }

            public void LoadMap(string filePath)
            {
                var lines = File.ReadAllLines(filePath);
                MapData = new float[GridHeight, GridWidth];
                startPositions.Clear();
                endPositions.Clear();  // Очистить списки перед загрузкой карты

                for (int y = 0; y < Math.Min(GridHeight, lines.Length); y++)
                {
                    var line = lines[y];
                    for (int x = 0; x < Math.Min(GridWidth, line.Length); x++)
                    {
                        char cell = line[x];
                        if (cell == '.')
                            MapData[y, x] = 0;  // Пустая ячейка
                        else if (cell == '#')
                            MapData[y, x] = 1;  // Препятствие
                        else if (cell == '2') // Стартовая точка
                        {
                            MapData[y, x] = 2;
                            startPositions.Add(new Point(x, y)); // Добавить в список стартовых точек
                        }
                        else if (cell == '3') // Конечная точка
                        {
                            MapData[y, x] = 3;
                            endPositions.Add(new Point(x, y)); // Добавить в список конечных точек
                        }
                        else
                            MapData[y, x] = 1;  // Недоступная ячейка
                    }
                }
            }

            public Bitmap DrawMap()
            {
                Map = new Bitmap(GridWidth * CellSize, GridHeight * CellSize);
                ConfigSpace = new Bitmap(GridWidth * CellSize, GridHeight * CellSize);

                using (Graphics g = Graphics.FromImage(Map))
                {
                    for (int y = 0; y < GridHeight; y++)
                    {
                        for (int x = 0; x < GridWidth; x++)
                        {
                            Color color;
                            switch (MapData[y, x])
                            {
                                case 0: color = Color.White; break;
                                case 1: color = Color.Black; break;
                                case 2: color = Color.LightGreen; break;
                                case 3: color = Color.LightBlue; break;
                                default: color = Color.Gray; break;
                            }

                            using (Brush brush = new SolidBrush(color))
                            {
                                g.FillRectangle(brush, x * CellSize, y * CellSize, CellSize, CellSize);
                            }
                            g.DrawRectangle(Pens.Gray, x * CellSize, y * CellSize, CellSize, CellSize);
                        }
                    }

                    foreach (var start in startPositions)
                    {
                        g.FillEllipse(Brushes.LightGreen, start.X * CellSize, start.Y * CellSize, CellSize, CellSize);
                    }
                    foreach (var end in endPositions)
                    {
                        g.FillEllipse(Brushes.LightBlue, end.X * CellSize, end.Y * CellSize, CellSize, CellSize);
                    }

                    return Map;
                }
            }

            public void ToggleCellState(int x, int y)
            {
                if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
                {
                    MapData[y, x] = MapData[y, x] == 0 ? 1 : 0;
                    DrawMap();
                }
            }

            public void SaveMap(string filePath)
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Сначала сохраняем карту
                    for (int y = 0; y < GridHeight; y++)
                    {
                        for (int x = 0; x < GridWidth; x++)
                        {
                            if (MapData[y, x] == 2)
                                writer.Write('2'); // Стартовая точка
                            else if (MapData[y, x] == 3)
                                writer.Write('3'); // Конечная точка
                            else
                                writer.Write(MapData[y, x] == 0 ? '.' : '#'); // Пустая ячейка или препятствие
                        }
                        writer.WriteLine();
                    }
                }
            }

            public List<Point> ConfigPoints = new List<Point>();

            public Bitmap DrawConfigurationSpace()
            {
                Graphics g = Graphics.FromImage(ConfigSpace);
                Pen gridPen = new Pen(Color.Black);

                for (int i = 0; i < GridWidth; i++)
                {
                    for (int j = 0; j < GridHeight; j++)
                    {
                        // Рисуем прямоугольник для каждой клетки
                        Rectangle cell = new Rectangle(i * CellSize, j * CellSize, CellSize, CellSize);
                        g.DrawRectangle(gridPen, cell);

                        // Проверяем, может ли робот встать в эту клетку с учетом радиуса
                        if (CanFitRobot(i, j))
                        {
                            ConfigPoints.Add(new Point(i, j));
                            // Закрашиваем клетку, если робот может туда поместиться
                            g.FillRectangle(Brushes.Lime, cell);  // Зеленый для доступных клеток
                        }
                        else
                        {
                            g.FillRectangle(Brushes.Gray, cell);  // Серая для недоступных клеток
                        }
                    }
                }

                return ConfigSpace;
            }

            public bool CanFitRobot(float i, float j)
            {
                if (j >= 0 && j < GridHeight && i >= 0 && i < GridWidth)
                {
                    // Проверить центральную точку клетки.
                    if (MapData[(int)j, (int)i] == 1)
                        return false;
                }

                // Проверить соседние клетки с учётом радиуса робота.
                for (float dy = (float)-RobotRadius / MiniCellSize; dy <= RobotRadius / MiniCellSize; dy += 0.1f)
                {
                    for (float dx = (float)-RobotRadius / MiniCellSize; dx <= RobotRadius / MiniCellSize; dx += 0.1f)
                    {
                        float nx = i + dx;
                        float ny = j + dy;

                        if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < GridHeight)
                        {
                            if (MapData[(int)ny, (int)nx] == 1)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
        }


            public class RandomSearch
            {
            public int StartX, StartY, EndX, EndY;
            public List<PointF> Path = new List<PointF>();
            HashSet<PointF> Visited = new HashSet<PointF>();
            Random Rand = new Random();
            PointF Current;
            PointF Start;
            public Bitmap Trajectory = mapOfField;
            public HashSet<PointF> deadEnds = new HashSet<PointF>();

            public RandomSearch(int startX, int startY, int endX, int endY)
            {
                StartX = startX;
                StartY = startY;
                EndX = endX;
                EndY = endY;
                Current = new PointF(StartX, StartY);

                Start = new PointF();
            }
            public List<PointF> StartSearch(MapViewer mapViewer, float robotRadius)
            {
                const int maxSteps = 100;
                int steps = 0;

                HashSet<PointF> visitedPoints = new HashSet<PointF>(); // Коллекция для отслеживания посещённых точек

                Stack<PointF> stack = new Stack<PointF>();
                stack.Push(Current); // Стартовая точка

                visitedPoints.Add(Current); // Добавляем стартовую точку в посещённые

                while (stack.Count > 0 && steps < maxSteps)
                {
                    Current = stack.Pop();

                    // Если точка достигнута, прерываем выполнение
                    if (Current.X == EndX && Current.Y == EndY)
                    {
                        Path.Add(Current); // Добавляем конечную точку в путь
                        break;
                    }

                    // Получаем соседей текущей точки
                    var neighbors = GetNeighbors(Current, mapViewer);

                    // Если у текущей точки нет соседей и она рядом с препятствием, это тупик
                    if (neighbors.Count == 0 && IsNearObstacle(Current, mapViewer, robotRadius))
                    {
                        deadEnds.Add(Current); // Добавляем точку в тупики
                        continue;
                    }

                    bool hasValidNeighbor = false;

                    // Проходим по всем соседям
                    foreach (var neighbor in neighbors)
                    {
                        // Если сосед свободен для робота и ещё не был посещен
                        if (CanFitRobotAtPosition(neighbor, robotRadius, mapViewer) && !visitedPoints.Contains(neighbor))
                        {
                            stack.Push(neighbor); // Добавляем соседа в стек для дальнейшего поиска
                            visitedPoints.Add(neighbor); // Отмечаем точку как посещенную
                            Path.Add(neighbor); // Добавляем точку в путь
                            hasValidNeighbor = true; // Сосед найден, путь продолжается
                        }
                    }

                    // Если у текущей точки нет валидных соседей, помечаем её как тупик
                    if (!hasValidNeighbor && IsNearObstacle(Current, mapViewer))
                    {
                        deadEnds.Add(Current);
                    }

                    steps++;
                }

                return Path;
            }

            // Функция для проверки, близка ли точка к препятствию
            private bool IsNearObstacle(PointF point, MapViewer mapViewer, float robotRadius = 1)
            {
                // Проверяем окружение точки, чтобы определить, является ли она тупиком, близким к препятствиям.
                var neighbors = GetNeighbors(point, mapViewer);
                foreach (var neighbor in neighbors)
                {
                    // Проверяем, есть ли вблизи препятствия
                    if (!CanFitRobotAtPosition(neighbor, robotRadius, mapViewer))
                    {
                        return true; // Если сосед является препятствием, точка рядом с препятствием
                    }
                }
                return false; // Если соседей рядом с препятствием нет, возвращаем false
            }

            public List<PointF> GetNeighbors(PointF current, MapViewer mapViewer)
            {
                List<PointF> neighbors = new List<PointF>();

                // Проверка на все возможные направления (вверх, вниз, влево, вправо)
                PointF[] directions = new PointF[]
                {
        new PointF(0, -1),  // вверх
        new PointF(0, 1),   // вниз
        new PointF(-1, 0),  // влево
        new PointF(1, 0)    // вправо
                };

                foreach (var dir in directions)
                {
                    PointF neighbor = new PointF(current.X + dir.X, current.Y + dir.Y);

                    // Добавляем соседа, если он в пределах карты и доступен для робота
                    if (CanFitRobotAtPosition(neighbor, (float)mapViewer.RobotRadius, mapViewer))
                    {
                        neighbors.Add(neighbor);
                    }
                }

                return neighbors;
            }

            // Функция, проверяющая, помещается ли робот на новую позицию с учетом радиуса
            public bool CanFitRobotAtPosition(PointF position, float robotRadius, MapViewer mapViewer)
            {
                // Определяем ограничивающий прямоугольник, который включает весь радиус робота
                int startX = (int)(position.X - robotRadius);
                int endX = (int)(position.X + robotRadius);
                int startY = (int)(position.Y - robotRadius);
                int endY = (int)(position.Y + robotRadius);

                // Проверяем все клетки в пределах окружности, соответствующей радиусу робота
                for (int x = startX; x <= endX; x++)
                {
                    for (int y = startY; y <= endY; y++)
                    {
                        // Проверяем, попадает ли клетка в круг с центром в position и радиусом robotRadius
                        if (Math.Sqrt(Math.Pow(x - position.X, 2) + Math.Pow(y - position.Y, 2)) <= robotRadius)
                        {
                            // Проверяем, что клетка свободна
                            if (!mapViewer.CanFitRobot(x, y)) // Если клетка занята
                                return false;
                        }
                    }
                }

                return true; // Все клетки в пределах радиуса свободны
            }


            // Функция для подсчета количества посещений точки
            private int CountVisits(PointF point)
            {
                return Path.Count(p => p.X == point.X && p.Y == point.Y);
            }

            private double GetDistanceToGoal(Point point)
            {
                // Используем Евклидово расстояние для определения ближайшего соседа
                return Math.Sqrt(Math.Pow(point.X - EndX, 2) + Math.Pow(point.Y - EndY, 2));
            }

            private List<PointF> AvoidObstacles(List<PointF> neighbors, int[] sensorData, int robotSpeed, MapViewer mapViewer)
            {
                List<PointF> safeNeighbors = new List<PointF>();
                int lookAheadDistance = 5; // Глубина проверки на несколько клеток вперед

                foreach (var neighbor in neighbors)
                {
                    if (IsSafePoint(neighbor, sensorData, mapViewer))
                    {
                        // Проверяем несколько шагов вперед
                        bool pathSafe = true;
                        for (int i = 1; i <= lookAheadDistance; i++)
                        {
                            // Рассчитываем следующую точку на траектории
                            float dx = neighbor.X - Current.X;
                            float dy = neighbor.Y - Current.Y;
                            float nextX = Current.X + i * dx;
                            float nextY = Current.Y + i * dy;

                            PointF nextPoint = new PointF(nextX, nextY);

                            // Проверяем каждую точку на безопасность
                            if (!IsSafePoint(nextPoint, sensorData, mapViewer))
                            {
                                pathSafe = false;
                                break;
                            }
                        }

                        if (pathSafe)
                        {
                            safeNeighbors.Add(neighbor);
                        }
                    }
                }

                return safeNeighbors;
            }

            private bool IsSafePoint(PointF point, int[] sensorData, MapViewer mapViewer)
            {
                // Проверка на границы карты
                if (point.X < 0 || point.X >= MapViewer.GridWidth || point.Y < 0 || point.Y >= MapViewer.GridHeight)
                    return false;

                // Проверка на препятствия в клетке
                if (mapViewer.MapData[(int)point.Y, (int)point.X] == 1)
                    return false;

                // Проверка на возможность разместить робота в клетке
                if (!mapViewer.CanFitRobot(point.X, point.Y))
                    return false;

                // Проверка данных сенсоров (пример для направления вперед)
                float distanceToPoint = Math.Abs(point.X - Current.X) + Math.Abs(point.Y - Current.Y);
                if (sensorData[0] < distanceToPoint * MapViewer.CellSize) // Условие для дальномера
                    return false;

                return true;
            }

            public void DrawPath(List<PointF> path, MapViewer mapViewer)
            {
                using (Graphics g = Graphics.FromImage(mapViewer.Map))
                using (Pen pen = new Pen(Color.Red, 3))  // Можно уменьшить толщину линии
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    // Рисуем путь между точками
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        PointF start = path[i];
                        PointF end = path[i + 1];

                        // Проверка, что клетки доступны для рисования
                        if (mapViewer.CanFitRobot(start.X, start.Y) && mapViewer.CanFitRobot(end.X, end.Y))
                        {
                            // Корректное отображение с учетом клеток
                            g.DrawLine(pen,
                                      start.X * MapViewer.CellSize + MapViewer.CellSize / 2,
                                      start.Y * MapViewer.CellSize + MapViewer.CellSize / 2,
                                      end.X * MapViewer.CellSize + MapViewer.CellSize / 2,
                                      end.Y * MapViewer.CellSize + MapViewer.CellSize / 2);
                        }
                    }
                }
            }

            public void DrawSearchVector(Graphics g, Point point, MapViewer mapViewer, Point next)
            {
                // Рисуем стрелку, показывающую направление робота
                int dx = next.X - point.X;
                int dy = next.Y - point.Y;
                int length = MapViewer.CellSize / 2;

                // Рисуем линию от текущей клетки к следующей
                g.DrawLine(Pens.Green,
                           point.X * MapViewer.CellSize + MapViewer.CellSize / 2,
                           point.Y * MapViewer.CellSize + MapViewer.CellSize / 2,
                           next.X * MapViewer.CellSize + MapViewer.CellSize / 2,
                           next.Y * MapViewer.CellSize + MapViewer.CellSize / 2);

                // Рисуем стрелку, указывающую направление
                g.DrawLine(Pens.Green,
                           next.X * MapViewer.CellSize + MapViewer.CellSize / 2,
                           next.Y * MapViewer.CellSize + MapViewer.CellSize / 2,
                           next.X * MapViewer.CellSize + MapViewer.CellSize / 2 - length,
                           next.Y * MapViewer.CellSize + MapViewer.CellSize / 2 - length);

                g.DrawLine(Pens.Green,
                           next.X * MapViewer.CellSize + MapViewer.CellSize / 2,
                           next.Y * MapViewer.CellSize + MapViewer.CellSize / 2,
                           next.X * MapViewer.CellSize + MapViewer.CellSize / 2 + length,
                           next.Y * MapViewer.CellSize + MapViewer.CellSize / 2 - length);
            }

            public Bitmap updatedMap = new Bitmap(mapOfField); // Создаем копию карты
            private int[,] stepVisitCount; // Используем двумерный массив для хранения посещений
            private const int RecentStepsToDraw = 10; // Количество последних шагов для отрисовки

            public Bitmap DrawSearchStep(PointF[] path, MapViewer mapViewer, int currentStepIndex, HashSet<PointF> deadEnds)
            {
                if (stepVisitCount == null)
                {
                    stepVisitCount = new int[mapViewer.Map.Width, mapViewer.Map.Height];
                }

                using (Graphics g = Graphics.FromImage(mapOfField))
                {
                    int startStepIndex = Math.Max(0, currentStepIndex - RecentStepsToDraw + 1);

                    // Создаем массив точек для отрисовки
                    List<RectangleF> rectanglesToDraw = new List<RectangleF>();
                    List<Brush> brushes = new List<Brush>();

                    // Рисуем путь
                    for (int i = startStepIndex; i <= currentStepIndex; i++)
                    {
                        var point = path[i];
                        int xIndex = (int)point.X;
                        int yIndex = (int)point.Y;

                        if (xIndex < 0 || xIndex >= mapViewer.Map.Width || yIndex < 0 || yIndex >= mapViewer.Map.Height)
                            continue;

                        stepVisitCount[xIndex, yIndex] = i;

                        Brush brush = (i < currentStepIndex) ? Brushes.Cyan : Brushes.Yellow;
                        rectanglesToDraw.Add(new RectangleF(xIndex * MapViewer.CellSize, yIndex * MapViewer.CellSize, MapViewer.CellSize, MapViewer.CellSize));
                        brushes.Add(brush);

                        // Проверяем, достигнут ли тупик (это будет точка, из которой не ведет путь)
                        if (IsNearObstacle(point, mapViewer))
                        {
                            // Добавляем тупик в список для отрисовки, если это тупик
                            Brush deadEndBrush = Brushes.Red;
                            rectanglesToDraw.Add(new RectangleF(xIndex * MapViewer.CellSize, yIndex * MapViewer.CellSize, MapViewer.CellSize, MapViewer.CellSize));
                            brushes.Add(deadEndBrush);
                        }
                    }

                    // Рисуем все точки (путь и тупики) за один цикл
                    for (int i = 0; i < rectanglesToDraw.Count; i++)
                    {
                        g.FillRectangle(brushes[i], rectanglesToDraw[i]);
                    }
                }

                return mapOfField;
            }

            private bool IsDeadEnd(PointF point, PointF[] path, MapViewer mapViewer)
            {
                // Получаем соседей текущей точки
                var neighbors = GetNeighbors(point, mapViewer);

                // Если точка не имеет соседей (или все соседи уже посещены), то это тупик
                return neighbors.Count == 0 || neighbors.All(n => path.Contains(n) || !CanFitRobotAtPosition(n, (float)mapViewer.RobotRadius, mapViewer));
            }

            private List<PointF> TrajectoryPoints = new List<PointF>(); // Хранит пройденные точки

            private List<PointF> FullTrajectory = new List<PointF>(); // Полный путь, включая пройденные точки и предсказанные

            public void DrawCurrentState(PointF currentPosition, List<PointF> path, int[] sensorData, int robotSpeed, MapViewer mapViewer)
            {
                // Рассчитать безопасный путь вперед
                List<PointF> safePath = AvoidObstacles(path, sensorData, robotSpeed, mapViewer);

                // Добавить текущую позицию в полный путь, если ее там еще нет
                if (!FullTrajectory.Contains(currentPosition))
                {
                    FullTrajectory.Add(currentPosition);
                }

                // Добавить только новые точки из safePath
                foreach (var point in safePath)
                {
                    if (!FullTrajectory.Contains(point))
                    {
                        FullTrajectory.Add(point);
                    }
                }

                // Используем слой для траектории
                using (Graphics g = Graphics.FromImage(Trajectory))
                {
                    // Рисуем каждую новую точку пути
                    for (int i = FullTrajectory.Count - safePath.Count - 1; i < FullTrajectory.Count; i++)
                    {
                        PointF point = FullTrajectory[i];

                        // Преобразование координат в пиксели
                        int pixelX = (int)(point.X * MapViewer.CellSize);
                        int pixelY = (int)(point.Y * MapViewer.CellSize);

                        // Рисуем точку траектории
                        g.FillRectangle(Brushes.Lime, pixelX, pixelY, MapViewer.CellSize, MapViewer.CellSize);
                    }

                    // Рисуем линии между точками
                    using (Pen pen = new Pen(Color.Blue, 3))
                    {
                        pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                        pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                        for (int i = FullTrajectory.Count - safePath.Count - 2; i < FullTrajectory.Count - 1; i++)
                        {
                            if (i < 0) continue; // Пропускаем если индекс отрицательный

                            PointF start = FullTrajectory[i];
                            PointF end = FullTrajectory[i + 1];

                            int startX = (int)(start.X * MapViewer.CellSize + MapViewer.CellSize / 2);
                            int startY = (int)(start.Y * MapViewer.CellSize + MapViewer.CellSize / 2);
                            int endX = (int)(end.X * MapViewer.CellSize + MapViewer.CellSize / 2);
                            int endY = (int)(end.Y * MapViewer.CellSize + MapViewer.CellSize / 2);

                            g.DrawLine(pen, startX, startY, endX, endY);
                        }
                    }
                }
            }

            public List<string> TranslatePathToCommands(List<PointF> path)
            {
                var commands = new List<string>();

                for (int i = 0; i < path.Count - 1; i++)
                {
                    PointF current = path[i];
                    PointF next = path[i + 1];

                    // Вычисляем направление
                    int direction = GetDirection(current, next);

                    // Формируем строку в формате "направление;X;Y"
                    commands.Add($"{direction};{next.X};{next.Y}");
                }

                // Добавляем конечную точку с направлением 0 (стоп)
                if (path.Any())
                {
                    PointF finalPoint = path[0];
                    commands.Add($"0;{finalPoint.X};{finalPoint.Y}");
                }

                return commands;
            }

            // Определение направления движения
            private static int GetDirection(PointF current, PointF next)
            {
                if (next.Y < current.Y) return 1; // Север
                if (next.X > current.X) return 2; // Восток
                if (next.Y > current.Y) return 3; // Юг
                if (next.X < current.X) return 4; // Запад
                return 0; // Стоп
            }

            // Сохранение команд в CSV файл
            public void SaveCommandsToCsv(List<string> commands)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                    saveFileDialog.DefaultExt = "csv";
                    saveFileDialog.Title = "Сохранение маршрута";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = saveFileDialog.FileName;
                        File.WriteAllLines(filePath, commands);
                        MessageBox.Show("Маршрут успешно сохранен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }

            private List<PointF> ReconstructPath(Dictionary<PointF, PointF> parents, PointF start, PointF end)
            {
                List<PointF> path = new List<PointF>();
                PointF current = end;

                // Строим путь от конца к началу
                while (!current.Equals(start))
                {
                    path.Add(current);
                    if (parents.ContainsKey(current))
                    {
                        current = parents[current];
                    }
                    else
                    {
                        // Если нет родителя, значит путь не существует
                        break;
                    }
                }

                path.Add(start);
                path.Reverse(); // Разворачиваем путь, так как мы шли от конца
                return path;
            }

            public List<PointF> FindOptimalPath(List<PointF> path, PointF start, PointF end, MapViewer mapViewer)
            {
                List<PointF> optimalPath = new List<PointF>();

                // Добавляем стартовую точку
                optimalPath.Add(start);

                // Начинаем с первой точки
                PointF currentPoint = start;

                // Проходим по всем точкам пути
                for (int i = 1; i < path.Count; i++)
                {
                    PointF nextPoint = path[i];

                    // Пробуем соединить текущую точку с точкой через несколько промежуточных
                    if (IsDirectPathClear(currentPoint, nextPoint, mapViewer))
                    {
                        // Если путь прямой и без препятствий, пропускаем промежуточные точки
                        currentPoint = nextPoint;
                    }
                    else
                    {
                        // Если путь не прямой, добавляем текущую точку в оптимальный путь
                        if (!IsNearObstacle(currentPoint, mapViewer)) // Проверяем, не тупик ли это
                        {
                            optimalPath.Add(currentPoint);
                        }
                        currentPoint = nextPoint;
                    }

                    // Если достигли конечной точки, добавляем её и завершаем
                    if (currentPoint.Equals(end))
                    {
                        optimalPath.Add(end);
                        break;
                    }
                }

                return optimalPath;
            }

            private bool IsDirectPathClear(PointF start, PointF end, MapViewer mapViewer)
            {
                // Проверяем, что путь либо горизонтальный, либо вертикальный
                if (start.X != end.X && start.Y != end.Y)
                {
                    return false;
                }

                // Если путь горизонтальный
                if (start.Y == end.Y)
                {
                    int minX = (int)Math.Min(start.X, end.X);
                    int maxX = (int)Math.Max(start.X, end.X);

                    for (int x = minX + 1; x < maxX; x++)
                    {
                        if (!mapViewer.CanFitRobot((int)start.Y, x)) // Проверка на препятствие
                        {
                            return false;
                        }
                    }
                }
                // Если путь вертикальный
                else if (start.X == end.X)
                {
                    int minY = (int)Math.Min(start.Y, end.Y);
                    int maxY = (int)Math.Max(start.Y, end.Y);

                    for (int y = minY + 1; y < maxY; y++)
                    {
                        if (!mapViewer.CanFitRobot(y, (int)start.X)) // Проверка на препятствие
                        {
                            return false;
                        }
                    }
                }

                // Если путь свободен
                return true;
            }

            public Bitmap DrawFinalPath(List<PointF> path, MapViewer mapViewer)
            {
                using (Graphics g = Graphics.FromImage(mapOfField))
                {
                    // Получаем оптимальный путь
                    List<PointF> optimizedPath = FindOptimalPath(path, startPoint, endPoint, mapViewer);

                    // Рисуем упрощенный путь
                    for (int i = 0; i < optimizedPath.Count - 1; i++)
                    {
                        g.DrawLine(Pens.Purple,
                            new PointF(optimizedPath[i].X * MapViewer.CellSize, optimizedPath[i].Y * MapViewer.CellSize),
                            new PointF(optimizedPath[i + 1].X * MapViewer.CellSize, optimizedPath[i + 1].Y * MapViewer.CellSize));
                    }

                    return mapOfField;
                }
            }


            // Метод для поиска соседей среди всех точек в списке
            public List<PointF> GetNeighbors(PointF p, List<PointF> allPoints, int direction)
            {
                List<PointF> neighbors = new List<PointF>();
                int[] dx = { 0, 0, -1, 1 };  // Направления по X (1 - вверх, 2 - вниз, 3 - влево, 4 - вправо)
                int[] dy = { -1, 1, 0, 0 };  // Направления по Y

                // Для поиска соседей по направлению
                for (int i = 0; i < 4; i++)
                {
                    // Вычисляем координаты соседа
                    float nx = p.X + dx[i];
                    float ny = p.Y + dy[i];

                    // Создаем точку соседа
                    PointF neighbor = new PointF(nx, ny);

                    // Проверяем, есть ли эта точка в списке всех точек
                    if (allPoints.Contains(neighbor))
                    {
                        neighbors.Add(neighbor);
                    }
                }

                return neighbors;
            }

            public List<PointF> GetCentralNeighbors(List<PointF> neighbors)
            {
                List<PointF> centralNeighbors = new List<PointF>();

                // Проверяем, что в списке есть хотя бы один сосед
                if (neighbors.Count == 0)
                    return centralNeighbors; // Возвращаем пустой список, если нет соседей

                // Выбираем точки с минимальным расстоянием от центральной точки (можно рассматривать все соседи)
                float minDistance = 10;

                foreach (var neighbor in neighbors)
                {
                    // Вычисляем расстояние от первого соседа (или центральной точки)
                    float distance = (float)Math.Sqrt(Math.Pow(neighbor.X - neighbors[0].X, 2) + Math.Pow(neighbor.Y - neighbors[0].Y, 2));

                    // Если расстояние меньше минимального, добавляем точку в список
                    if (distance < minDistance)
                    {
                        centralNeighbors.Add(neighbor);
                        minDistance = distance; // Обновляем минимальное расстояние
                    }
                }

                return centralNeighbors;
            }

            // Функция для расчета Евклидова расстояния между двумя точками
            private double GetDistance(Point p1, Point p2)
            {
                return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            }

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            string solutionDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            string filePath = Path.Combine(solutionDirectory, "textbox_data.json");

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<dynamic>(json);

                textBox1.Text = data.TextBox1;
                textBox2.Text = data.TextBox2;
                textBox3.Text = data.TextBox3;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            timer1.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form form = new ViewFromRobot();
            form.Show();
        }

        public void SplitDataToTextBoxs()
        {
            var message = Encoding.ASCII.GetString(server.Data);
            var text = JsonConvert.DeserializeObject<Dictionary<string, int>>(message);

            foreach (var chr in text)
            {
                if (chr.Key == "d0")
                {
                    textBox4.Text = chr.Value.ToString();
                }
                if (chr.Key == "d1")
                {
                    textBox5.Text = chr.Value.ToString();
                }
                if (chr.Key == "d2")
                {
                    textBox6.Text = chr.Value.ToString();
                }
                if (chr.Key == "d3")
                {
                    textBox7.Text = chr.Value.ToString();
                }
                if (chr.Key == "d4")
                {
                    textBox8.Text = chr.Value.ToString();
                }
                if (chr.Key == "d5")
                {
                    textBox9.Text = chr.Value.ToString();
                }
                if (chr.Key == "d6")
                {
                    textBox10.Text = chr.Value.ToString();
                }
                if (chr.Key == "d7")
                {
                    textBox11.Text = chr.Value.ToString();
                }

                if (chr.Key == "n")
                {
                    textBox12.Text = chr.Value.ToString();
                }
                if (chr.Key == "s")
                {
                    textBox13.Text = chr.Value.ToString();
                }
                if (chr.Key == "c")
                {
                    textBox14.Text = chr.Value.ToString();
                }
                if (chr.Key == "re")
                {
                    textBox15.Text = chr.Value.ToString();
                }
                if (chr.Key == "le")
                {
                    textBox16.Text = chr.Value.ToString();
                }
                if (chr.Key == "az")
                {
                    textBox17.Text = chr.Value.ToString();
                }
                if (chr.Key == "b")
                {
                    textBox18.Text = chr.Value.ToString();
                }
                if (chr.Key == "l0")
                {
                    textBox24.Text = chr.Value.ToString();
                }
                if (chr.Key == "l1")
                {
                    textBox23.Text = chr.Value.ToString();
                }
                if (chr.Key == "l2")
                {
                    textBox22.Text = chr.Value.ToString();
                }
                if (chr.Key == "l3")
                {
                    textBox21.Text = chr.Value.ToString();
                }
                if (chr.Key == "l4")
                {
                    textBox20.Text = chr.Value.ToString();
                }
            }
        }

        public static void RotateToAngle(PointF current, PointF target, PointF prev)
        {
            // Вычисление угла к целевой точке
            double angleToTarget = CalculateTurnAngleToPoint(current, target);

            // Текущий угол робота (например, относительно предыдущей точки)
            double currentAngle = CalculateTurnPoint(current, prev);

            // Разница углов с нормализацией
            double angleDiff = NormalizeAngle(angleToTarget - currentAngle);

            // Минимальная и максимальная скорости поворота
            double minTurnSpeed = 10;
            double maxTurnSpeed = 25;

            // Скорость поворота на основе угла разницы
            double turnSpeed = Math.Min(Math.Abs(angleDiff), maxTurnSpeed);

            // Применение минимальной скорости для малых углов
            if (Math.Abs(angleDiff) < 30)
            {
                turnSpeed = Math.Max(turnSpeed, minTurnSpeed);
            }

            // Команды поворота в сторону минимального угла
            if (angleDiff > 0) // Поворот вправо
            {
                Robot.SetCommand("B", (int)turnSpeed);  // Поворот вправо
                Robot.SetCommand("F", 0);              // Остановка движения
            }
            else if (angleDiff < 0) // Поворот влево
            {
                Robot.SetCommand("B", -(int)turnSpeed);  // Поворот влево
                Robot.SetCommand("F", 0);                // Остановка движения
            }
        }

        // Метод нормализации угла в диапазон [-180°, 180°]
        private static double NormalizeAngle(double angle)
        {
            while (angle > 180)
                angle -= 360;
            while (angle < -180)
                angle += 360;
            return angle;
        }

        // Пример методов для вычисления углов
        private static double CalculateTurnAngleToPoint(PointF current, PointF target)
        {
            double deltaX = target.X - current.X;
            double deltaY = target.Y - current.Y;
            return Math.Atan2(deltaY, deltaX) * (180.0 / Math.PI); // Угол в градусах
        }

        private static double CalculateTurnPoint(PointF current, PointF prev)
        {
            double deltaX = current.X - prev.X;
            double deltaY = current.Y - prev.Y;
            return Math.Atan2(deltaY, deltaX) * (180.0 / Math.PI); // Угол в градусах
        }


        public void ManualMotion()
        {
            // Получаем текущее значение скорости вперёд и назад
            int currentForwardSpeed = trackBar1.Value;
            int currentBackwardSpeed = trackBar2.Value;

            // Проверяем, изменилась ли скорость вперёд
            if (currentForwardSpeed != previousForwardSpeed)
            {
                if (currentForwardSpeed > 0)
                {
                    Robot.SetCommand("B", 0); // Обнуляем поворот
                    visualization.isTurning = false; // Завершаем поворот
                }
                previousBackwardSpeed = 0; // Обновляем переменную для отслеживания
            }

            // Устанавливаем текущие команды
            Robot.SetCommand("F", currentForwardSpeed);

            // Если скорость "F" равна 0, то устанавливаем команду для поворота
            if (currentForwardSpeed == 0)
            {
                Robot.SetCommand("B", currentBackwardSpeed);
            }
            else
            {
                Robot.SetCommand("B", 0); // Обнуляем поворот, если скорость не равна 0
            }

            // Обновляем предыдущие скорости
            previousForwardSpeed = currentForwardSpeed;

            // Если флаг isSend активен, отправляем команды из текстовых полей
            if (isSend)
            {
                int forwardSpeedFromText = Convert.ToInt32(textBox26.Text);
                int backwardSpeedFromText = Convert.ToInt32(textBox27.Text);

                // Проверяем, изменилась ли скорость вперёд
                if (forwardSpeedFromText != previousForwardSpeed)
                {
                    if (currentForwardSpeed > 0)
                    {
                        Robot.SetCommand("B", 0); // Обнуляем поворот
                        visualization.isTurning = false; // Завершаем поворот
                                                         //visualization.ResetPositionAfterTurn();
                    }
                    previousBackwardSpeed = 0; // Обновляем переменную для отслеживания
                }

                // Устанавливаем команду движения вперёд
                Robot.SetCommand("F", forwardSpeedFromText);

                // Если скорость "F" равна 0, разрешаем поворот
                if (forwardSpeedFromText == 0)
                {
                    Robot.SetCommand("B", backwardSpeedFromText); // Устанавливаем поворот
                }
                else
                {
                    Robot.SetCommand("B", 0); // Обнуляем поворот, если скорость не равна 0
                }
            }

            // Обновляем предыдущие скорости
            previousForwardSpeed = currentForwardSpeed;
        }

        public static bool IsThereStartAndEndPoint(MapViewer mapViewer)
        {
            if (mapViewer.MapData != null)
            {
                for (int y = 0; y < MapViewer.GridHeight; y++)
                {
                    for (int x = 0; x < MapViewer.GridWidth; x++)
                    {
                        if (mapViewer.MapData[y, x] == 2)
                        {
                            startPoint = new Point(x, y);
                        }
                        if (mapViewer.MapData[y, x] == 3)
                        {
                            endPoint = new Point(x, y);
                        }
                    }
                }

                if (startPoint != null && endPoint != null)
                {
                    return true;
                }
            }

            return false;
        }

        public static double CalculateTurnAngle(double V, double L, double deltaTime)
        {
            // Вычисляем угловую скорость ω = (vR - vL) / L
            double omega = V / L;

            // Вычисляем угол поворота θ = ω * deltaTime
            double angleInRadians = omega * deltaTime;

            // Переводим радианы в градусы, если нужно
            double angleInDegrees = angleInRadians * (180.0 / Math.PI);

            return angleInDegrees;  // Возвращаем угол в градусах
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            if (UDPServer.DecodeData != null)
            {
                if (checkBox1.Checked)
                {
                    ManualMotion();
                }
                else
                {
                    int[] sensorData = { UDPServer.d0, UDPServer.d1, UDPServer.d2, UDPServer.d3, UDPServer.d4, UDPServer.d5, UDPServer.d6,
                UDPServer.d7 };
                    float[] sensorsAngles = { 0, 45, 90, 135, 180, -135, -90, -45 };

                    Robot.MoveAlongPath(randomSearch.TranslatePathToCommands(randomSearch.Path), vis);
                    //randomSearch.DrawCurrentState(vis.RobotPosition, randomSearch.Path, sensorData, 5, mapViewer);
                    

                    textBox25.Text = angleDiff.ToString();
                }

                SplitDataToTextBoxs();

                richTextBox1.Text = "\r\n" + "Here is data";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();

                visualization.DetectObstaclesFromSensors(threshold, UDPServer.d0, UDPServer.d1, UDPServer.d2, UDPServer.d3,
                        UDPServer.d4, UDPServer.d5, UDPServer.d6, UDPServer.d7);
                bitmap = visualization.DrawRobot((int)CalculateTurnAngle(Robot.Commands["B"], 1, count / 10));
                MiniMap = visualization.DrawMiniMap();
                count++;
                richTextBox1.Text = "\r\n" + vis.RobotPosition.X.ToString() + "; " + vis.RobotPosition.Y.ToString();
                richTextBox1.Text = richTextBox1.Text + "\r\n" + (randomSearch.Path[Robot.index].X).ToString() + "; " 
                    + (randomSearch.Path[Robot.index].Y).ToString();

                Robot.SetCommand("N", count);

                // Обновляем данные робота
                Robot.UpdateDecodeText();
                Robot.SendOldCommands();
                await server.SendRobotDataAsync();

                textBox19.Text = count.ToString();
                
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            server = new UDPServer(IPAddress.Parse(textBox3.Text), Int32.Parse(textBox2.Text), Int32.Parse(textBox1.Text));
            await server.ReceiveDataAsync();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var data = new
            {
                TextBox1 = textBox1.Text,
                TextBox2 = textBox2.Text,
                TextBox3 = textBox3.Text
            };

            string solutionDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
            string filePath = Path.Combine(solutionDirectory, "textbox_data.json");

            string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            label29.Text = trackBar3.Value.ToString();
            threshold = trackBar3.Value;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label25.Text = trackBar1.Value.ToString();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            label26.Text = trackBar2.Value.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            isSend = true;
            countOfClick++;
            if (countOfClick % 2 == 0)
            {
                isSend = false;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            mapViewer.OpenMap();
        }

        private void textBox25_TextChanged(object sender, EventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            mapOfField = mapViewer.DrawMap();
            configImage = mapViewer.DrawConfigurationSpace();

            Form form = new Map();
            form.Show();

            if (IsThereStartAndEndPoint(mapViewer))
            {
                //randomSearch = new RandomSearch(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y);

                vis.RobotPosition = new PointF(startPoint.X, startPoint.Y);
                visualization.RobotPosition = new PointF(205, 140);
                timer2.Enabled = true;
                timer2.Start();
            }

            richTextBox1.Text = startPoint.X.ToString() + "; " + startPoint.Y.ToString() + " " + endPoint.X.ToString() + "; " 
                + endPoint.Y.ToString();

            

           
            //randomSearch.DrawPath(randomSearch.Path, mapViewer);
            //randomSearch.DrawSearchStep(randomSearch.Path, mapViewer);
            //randomSearch.DrawPath(randomSearch.Path, mapViewer);
        }
    }
}
