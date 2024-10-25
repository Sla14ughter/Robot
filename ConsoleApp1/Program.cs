using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Security.Principal;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.CompilerServices;

namespace ConsoleApp1
{
    internal class Program
    {
        const string token = "b8af64b2-d3cb-4006-9fb0-a4427f1c1b8d043f1012-817c-4ba4-ae12-e3bb489b88bf";
        private static Random rnd = new Random();
        private static readonly HttpClient client = new HttpClient();
        private static Cell[,] map = new Cell[16, 16];
        private static int posX = 0;
        private static int posY = 15;
        private static bool facingX = false;
        private static bool facingPositive = false;
        private static bool notComplete = true;
        // Внимание! Я написал весь код так, что восток находится справа. 
        /* Памятка по моей всратой системе координат 
         * 
         *       N
         *       -
         *   E - 0 + W -> x
         *       +
         *       S
         *       ↓ y
         */
        static async Task Main()
        {
            bool DebugMode = false;
            for (int i = 0; i < map.GetLength(0); i++)
                for (int j = 0; j < map.GetLength(1); j++)
                    map[i, j] = new Cell();
            if (!DebugMode)
            {
                await Initialize();
                while (notComplete)
                {
                    await Proceed();
                    await DecideWhereToGo();
                }
            }
            await SendMap();
            Console.ReadLine();
        }

        static async Task SendMap()
        {
            int l0 = map.GetLength(0);
            int l1 = map.GetLength(1);
            int[,] result = new int[l0, l1];
            for (int i = 0; i < l0; i++)
                for (int j = 0; j < l1; j++)
                    result[i, j] = map[i, j].ToInt();
            string body = JsonConvert.SerializeObject(result);
            StringContent content = new StringContent(body, Encoding.UTF8, "application/json");
            Console.WriteLine(content);
            HttpResponseMessage message = await client.PostAsync($"http://127.0.0.1:8801/api/v1/matrix/send?token={token}", content);
            if (message.IsSuccessStatusCode)
            {
                Console.WriteLine("Yippeeeeee!");
            }
            else
            {
                Console.WriteLine("tf wrong with u?");
            }
            Console.WriteLine(body);
        }

        private static async Task Initialize()
        {
            await UpdateMap();
            Cell current = map[posY, posX];
            if(current.north == 3) await TurnRight();
            await GoForward();
        }

        private static async Task DecideWhereToGo()
        {
            Cell current = map[posY, posX];
            List<int[]> possibilities = new List<int[]>();
            for (int i = 0; i < current.Props.Length; i++) 
                if (current.Props[i] < 2)
                    possibilities.Add(new int[] {i, current.Props[i]});
            for(int i = 1; i > -1; i--)
                if (possibilities.Exists(p => p[1] == i))
                    possibilities.RemoveAll(p => p[1] > i);
            int next = rnd.Next(possibilities.Count);
            switch (possibilities[next][0])
            {
                case 0:
                    await TurnEast();
                    break;
                case 1:
                    await TurnNorth();
                    break;
                case 2:
                    await TurnWest();
                    break;
                case 3:
                    await TurnSouth();
                    break;
            }
            LeaveMarkAhead();
            await GoForward();
        }

        private static void LeaveMarkAhead()
        {
            if (facingX)
            {
                if (facingPositive)
                {
                    if (map[posY, posX].west < 2)
                        map[posY, posX].west++;
                }
                else
                {
                    if (map[posY, posX].east < 2)
                        map[posY, posX].east++;
                }
            }
            else
            {
                if (facingPositive)
                {
                    if (map[posY, posX].south < 2)
                        map[posY, posX].south++;
                }
                else
                {
                    if (map[posY, posX].north < 2)
                        map[posY, posX].north++;
                }
            }
        }

        private static void LeaveMarkBehind()
        {
            if(facingX)
            {
                if (facingPositive)
                {
                    if (map[posY, posX].east < 2)
                        map[posY, posX].east++;
                }
                else
                {
                    if (map[posY, posX].west < 2)
                        map[posY, posX].west++;
                }      
            }
            else
            {
                if (facingPositive)
                {
                    if (map[posY, posX].north < 2)
                        map[posY, posX].north++;
                }
                else
                {
                    if (map[posY, posX].south < 2)
                        map[posY, posX].south++;
                }
            }
        }


        /// <summary>
        /// Робот проходит коридор вперед до следующей развилки
        /// </summary>
        private static async Task Proceed()
        {
            while (notComplete)
            {
                await UpdateMap();
                switch (map[posY, posX].ToInt())
                {
                    case 5:
                        if (!facingX && facingPositive) await TurnLeft();
                        else await TurnRight();
                        break;
                    case 6:
                        if (!facingX && facingPositive) await TurnRight();
                        else await TurnLeft();
                        break;
                    case 7:
                        if (!facingX && !facingPositive) await TurnLeft();
                        else await TurnRight();
                        break;
                    case 8:
                        if (!facingX && !facingPositive) await TurnRight();
                        else await TurnLeft();
                        break;
                    case 9:
                        break;
                    case 10:
                        break;
                    case 11:
                        await TurnRight();
                        await TurnRight();
                        break;
                    case 12:
                        await TurnRight();
                        await TurnRight();
                        break;
                    case 13:
                        await TurnRight();
                        await TurnRight();
                        break;
                    case 14:
                        await TurnRight();
                        await TurnRight();
                        break;
                    default:
                        LeaveMarkBehind();
                        return;
                }
                await GoForward();
                if (posX == 0 && posY == 15) notComplete = false;
            }
        }

        private static async Task TurnNorth()
        {
            if (!facingX && facingPositive)
            {
                if (rnd.Next(2) == 0) await TurnLeft();
                else await TurnRight();
            }
            if (facingX)
            {
                if (facingPositive) await TurnLeft();
                else await TurnRight();
            }
        }

        private static async Task TurnSouth()
        {
            if (!facingX && !facingPositive)
            {
                if (rnd.Next(2) == 0) await TurnRight();
                else await TurnLeft();
            }
            if (facingX)
            {
                if (facingPositive) await TurnRight();
                else await TurnLeft();
            }
        }
        private static async Task TurnWest()
        {
            if (facingX && !facingPositive)
            {
                if (rnd.Next(2) == 0) await TurnLeft();
                else await TurnRight();
            }
            if (!facingX)
            {
                if (facingPositive) await TurnLeft();
                else await TurnRight();
            }
        }

        private static async Task TurnEast()
        {
            if (facingX && facingPositive)
            {
                if (rnd.Next(2) == 0) await TurnRight();
                else await TurnLeft();
            }
            if (!facingX)
            {
                if (facingPositive) await TurnRight();
                else await TurnLeft();
            }
        }

        private static async Task TurnRight()
        {
            StringContent content = new StringContent("", Encoding.UTF8, "application/json");
            HttpResponseMessage message = await client.PostAsync($"http://127.0.0.1:8801/api/v1/robot-cells/right?token={token}", content);
            if(message.IsSuccessStatusCode)
            {
                facingX = !facingX;
                if (facingX) facingPositive = !facingPositive;
            }
            else
            {
                Console.WriteLine("tf wrong with u?");
            }
        }
        private static async Task GoForward()
        {
            StringContent content = new StringContent("", Encoding.UTF8, "application/json");
            HttpResponseMessage message = await client.PostAsync($"http://127.0.0.1:8801/api/v1/robot-cells/forward?token={token}", content);
            if (message.IsSuccessStatusCode)
            {
                if (facingX)
                    posX += facingPositive ? 1 : -1;
                else
                    posY += facingPositive ? 1 : -1;
            }
            else
            {
                Console.WriteLine("tf wrong with u?");
            }
        }
        private static async Task TurnLeft()
        {
            StringContent content = new StringContent("", Encoding.UTF8, "application/json");
            HttpResponseMessage message = await client.PostAsync($"http://127.0.0.1:8801/api/v1/robot-cells/left?token={token}", content);
            if (message.IsSuccessStatusCode)
            {
                facingX = !facingX;
                if (!facingX) facingPositive = !facingPositive;
            }
            else
            {
                Console.WriteLine("tf wrong with u?");
            }
        }
        private static async Task GoBackward()
        {
            StringContent content = new StringContent("", Encoding.UTF8, "application/json");
            HttpResponseMessage message = await client.PostAsync($"http://127.0.0.1:8801/api/v1/robot-cells/backward?token={token}", content);
            if (message.IsSuccessStatusCode)
            {
                if (facingX)
                    posX += facingPositive ? -1 : 1;
                else
                    posY += facingPositive ? -1 : 1;
            }
            else
            {
                Console.WriteLine("tf wrong with u?");
            }
        }


        /// <summary>
        /// Метод для сканирования окружения.
        /// Заполняет карту в зависимости от увиденного.
        /// </summary>
        private static async Task UpdateMap() // I can see forever!
        {
            HttpResponseMessage response = await client.GetAsync($"http://127.0.0.1:8801/api/v1/robot-cells/sensor-data?token={token}");
            if (response.IsSuccessStatusCode)
            {
                // получаем данные из запроса
                string body = await response.Content.ReadAsStringAsync();
                SensorData data = JsonConvert.DeserializeObject<SensorData>(body);
                // определяем расстояние до стен
                int[] distances = new int[4]
                {
                    (int)Math.Floor(data.front_distance / 166),
                    (int)Math.Floor(data.back_distance / 166),
                    (int)Math.Floor(data.right_side_distance / 166),
                    (int)Math.Floor(data.left_side_distance / 166),
                };
                for(int i = 0; i < distances.Length; i++)
                {
                    // определяем глобальное направление скана на основе своего направления
                    bool directionX = facingX ^ i > 1;
                    bool directionPositive = facingPositive ^ i % 2 != 0 ^ (!facingX & i > 1);
                    // размечаем стены
                    int tileX;
                    int tileY;
                    if (!directionPositive) distances[i] *= -1;
                    if (directionX)
                    {
                        tileX = posX + distances[i];
                        tileY = posY;
                        if (directionPositive)
                        {
                            map[tileY, tileX].west = 3;
                            if (tileX + 1 < 16) map[tileY, tileX + 1].east = 3;
                        }
                        else
                        {
                            map[tileY, tileX].east = 3;
                            if (tileX - 1 > 0) map[tileY, tileX - 1].west = 3;
                        }
                    }
                    else
                    {
                        tileX = posX;
                        tileY = posY + distances[i];
                        if (directionPositive)
                        {
                            map[tileY, tileX].south = 3;
                            if (tileY + 1 < 16) map[tileY + 1, tileX].north = 3;
                        }
                        else
                        {
                            map[tileY, tileX].north = 3;
                            if (tileY - 1 > 0) map[tileY - 1, tileX].south = 3;
                        }
                    }
                }
            }
        }
    }

    internal class SensorData
    {
        public float front_distance;
        public float right_side_distance;
        public float left_side_distance;
        public float back_distance;
        public float left_45_distance;
        public float right_45_distance;
        public float rotation_pitch;
        public float rotation_yaw;
        public float rotation_roll;
        public float down_x_offset;
        public float down_y_offset;
    }

    internal class Cell
    {
        /* status
         * 0 no wall
         * 1 mark
         * 2 dead end
         * 3 wall
         */
        public int east = 0;
        public int north = 0;
        public int west = 0;
        public int south = 0;

        public int[] Props { get => new int[4] { east, north, west, south }; }

        private static string[] regex = new string[]
            {
                @"^[0-2]{4}$",
                @"^3[0-2]{3}$",
                @"^[0-2]3[0-2]{2}$",
                @"^[0-2]{2}3[0-2]$",
                @"^[0-2]{3}3$",
                @"^3[0-2]{2}3$",
                @"^[0-2]{2}3{2}$",
                @"^[0-2]3{2}[0-2]$",
                @"^3{2}[0-2]{2}$",
                @"^3[0-2]3[0-2]$",
                @"^[0-2]3[0-2]3$",
                @"^[0-2]3{3}$",
                @"^3{3}[0-2]$",
                @"^3{2}[0-2]3$",
                @"^3[0-2]3{2}$"
            };

        public int ToInt()
        {
            for (int i = 0; i < regex.Length; i++)
                if (Regex.IsMatch($"{east}{north}{west}{south}", regex[i]))
                    return i;
            return 15;
        }
    }
}
