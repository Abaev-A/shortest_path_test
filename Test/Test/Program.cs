using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace Test
{
    class Program
    {
        static private int bus_count, all_stop_count, start_time, start_stop, finish_stop;
        private const int MAX_TIME = 1440; // 24 часа в минутах. Окончание рабочего дня
        static private int[] work_time, price;
        static private Route[] result = new Route[2];
        static private Graph[] graphs;

        // Списки с номерами маршрутов, которые включают начальную и конечную остановку
        static private List<int> start_point_graphs = new List<int>();
        static private List<int> finish_point_graphs = new List<int>();

        static void Main(string[] args)
        {
            ParseInitArgs(args);

            FindShortestRoute();
            if (result[1] == null)
            {
                Console.WriteLine("Самый короткий путь вышел за рамки рабочего времени. Ответов нет!\n");
                Console.ReadLine();
                return;
            }
            FindCheapestRoute();

            if (RoutePrice(result[1]) == RoutePrice(result[0])){
                result[1] = result[0];
            }
            else if (RouteTime(result[1]) == RouteTime(result[0])){
                result[0] = result[1];
            }

            Console.WriteLine("Самый короткий по времени маршрут:\n\n" + result[0].Description());
            Console.WriteLine("Самый дешевый маршрут:\n\n" + result[1].Description());
            Console.ReadLine();
        }

        private struct Graph // В матричном представлении
        {
            public int stop_count, full_circle;
            public int[] stop;
            public int[,] matrx;
            public void Init(string graph_raw)
            {
                string[] tmp = graph_raw.Split(' ');
                stop_count = int.Parse(tmp[0]);
                stop = new int[stop_count];

                for (int i = 0; i < stop_count; i++)
                {
                    stop[i] = int.Parse(tmp[i + 1]) - 1;
                }

                full_circle = 0;
                matrx = new int[all_stop_count, all_stop_count];

                for (int i = 0; i < stop_count - 1; i++)
                {
                    full_circle += matrx[stop[i], stop[i + 1]] = int.Parse(tmp[1 + stop_count + i]);
                }
                full_circle += matrx[stop[stop_count - 1], stop[0]] = int.Parse(tmp[stop_count * 2]);
            }
        }

        private class Pair<T1, T2> // Изменяемый аналог Tuple 
        {
            public T1 First { get; set; }
            public T2 Second { get; set; }
        }

        private class Route // Описание маршрута упорядоченными парами вида {номер автобуса, номер остановки}
        {
            public struct Node
            {
                int bus;
                int stop;

                public Node(int x, int y)
                {
                    bus = x;
                    stop = y;
                }
                public int Bus
                {
                    get
                    {
                        return bus;
                    }
                    set
                    {
                        bus = value;
                    }
                }
                public int Stop
                {
                    get
                    {
                        return stop;
                    }
                    set
                    {
                        stop = value;
                    }
                }
            }

            private int weight;
            public List<Node> arr = new List<Node>();

            public int Weight
            {
                get
                {
                    return weight;
                }
                set
                {
                    if (value > 0)
                    {
                        weight = value;
                    }
                }
            }
            public Route Copy()
            {
                Route tmp = new Route();
                tmp.Weight = this.Weight;
                foreach (Node elem in this.arr)
                {
                    tmp.arr.Add(elem);
                }

                return tmp;
            }
            public string Description()
            {
                string tmp = "";
                List<int> log = new List<int>();
                int time = RouteTime(this, log: log);

                tmp += string.Format("\t{0} Начало. Ждем!\n", TimeSpan.FromMinutes(start_time).ToString(@"hh\:mm"));
                tmp += string.Format("\t{0} Посадка на остановке {1}, автобус {2}\n", TimeSpan.FromMinutes(log[0]).ToString(@"hh\:mm"), arr[0].Stop + 1, arr[0].Bus + 1);

                for (int i = 1; i < arr.Count; i++)
                {
                    if (arr[i].Bus != arr[i - 1].Bus)
                    {
                        tmp += string.Format("\t{0} Посадка на остановке {1} в автобус {2}\n", TimeSpan.FromMinutes(log[i]).ToString(@"hh\:mm"), arr[i].Stop + 1, arr[i].Bus + 1);
                    }
                    else
                    {
                        if (i != arr.Count - 1 && arr[i].Bus != arr[i + 1].Bus)
                        {
                            tmp += string.Format("\t{0} Выходим на остановке {1}, ждем автобус {2}\n", TimeSpan.FromMinutes(log[i]).ToString(@"hh\:mm"), arr[i].Stop + 1, arr[i + 1].Bus + 1);
                        }
                        else
                        {
                            tmp += string.Format("\t{0} Остановка {1}\n", TimeSpan.FromMinutes(log[i]).ToString(@"hh\:mm"), arr[i].Stop + 1);
                        }
                    }
                }

                tmp += string.Format("\nСтоимость проезда: {0} рублей\n", RoutePrice(this));
                tmp += string.Format("Затраченное время: {0}\n", TimeSpan.FromMinutes(time - start_time).ToString(@"hh\:mm"));
                return tmp;
            }
            public void Add(Route x)
            {
                for (int i = 0; i < x.arr.Count; i++)
                {
                    this.arr.Add(x.arr[i]);
                }
            }
        }

        private static void ParseInitArgs(string[] args)
        {
            start_stop = int.Parse(args[1]) - 1;
            finish_stop = int.Parse(args[2]) - 1;
            start_time = int.Parse(args[3].Split(':')[0]) * 60 + int.Parse(args[3].Split(':')[1]);

            using (StreamReader fs = new StreamReader(args[0]))
            {
                bus_count = int.Parse(fs.ReadLine());
                all_stop_count = int.Parse(fs.ReadLine());

                string[] tmp = new string[bus_count];
                work_time = new int[bus_count];
                price = new int[bus_count];
                graphs = new Graph[bus_count];

                tmp = fs.ReadLine().Split(' ');

                for (int i = 0; i < bus_count; i++) //Переводим время в минуты
                {
                    work_time[i] = int.Parse(tmp[i].Split(':')[0]) * 60 + int.Parse(tmp[i].Split(':')[1]);
                }

                tmp = fs.ReadLine().Split(' ');

                for (int i = 0; i < bus_count; i++)
                {
                    price[i] = int.Parse(tmp[i]);
                }

                for (int i = 0; i < bus_count; i++)
                {
                    graphs[i].Init(fs.ReadLine());
                }
            }

            for (int i = 0; i < bus_count; i++)
            {
                if (Array.Exists(graphs[i].stop, element => element == start_stop))
                {
                    start_point_graphs.Add(i);
                }
                if (Array.Exists(graphs[i].stop, element => element == finish_stop))
                {
                    finish_point_graphs.Add(i);
                }
            }
        }

        private static void FindShortestRoute()
        {
            // Расширенный граф, дополнительными вершинами которого являются пересекающиеся остановки
            Graph graph_ext = new Graph();
            // Исходные маршруты в виде матрицы с дополнительынми вершинами
            Graph[] graphs2 = new Graph[bus_count];
            int ext_count = 0;

            for (int i = 0; i < bus_count; i++) ext_count += graphs[i].stop_count;
            graph_ext.matrx = new int[ext_count, ext_count];
            graph_ext.stop_count = ext_count;
            graph_ext.stop = Enumerable.Range(0, ext_count).Select(x => x).ToArray();  //////// ???????????????/ нужно ли
            for (int i = 0; i < bus_count; i++)
            {
                graphs2[i].matrx = new int[ext_count, ext_count];
                graphs2[i].stop_count = graphs[i].stop_count;
                graphs2[i].stop = new int[graphs2[i].stop_count];
                Array.Copy(graphs[i].stop, graphs2[i].stop, graphs[i].stop_count);
            }

            // Массивы, конвертирующие номер остановки в автобус и в номер остановки относительно старой нумерации
            int[] conv_bus = new int[ext_count];
            int[] conv_stop = new int[ext_count];
            for (int i = 0; i < all_stop_count; i++) conv_stop[i] = i;

            // Список с повторениями остановок с уточнением маршрутов 
            List<Pair<int, List<int>>> repeats = new List<Pair<int, List<int>>>();

            for (int s = 0; s < all_stop_count; s++)
            {
                repeats.Add(new Pair<int, List<int>>());
                repeats[s].First = s;
                repeats[s].Second = new List<int>();
                for (int g = 0; g < bus_count; g++)
                {
                    if (Array.Exists(graphs[g].stop, element => element == s)) repeats[s].Second.Add(g);
                }
            }
            for (int i = 0, tmp = all_stop_count, iter = all_stop_count; i < tmp; i++)
            {
                if (repeats[i].Second.Count == 1)
                {
                    conv_bus[repeats[i].First] = repeats[i].Second[0];
                    repeats.RemoveAt(i);
                    tmp--;
                    i--;
                }
                else
                {
                    for (int x = 0; x < repeats[i].Second.Count; x++)
                    {
                        if (x != 0)
                        {
                            conv_stop[iter] = repeats[i].First;
                            conv_bus[iter++] = repeats[i].Second[x];
                        }
                        else
                            conv_bus[repeats[i].First] = repeats[i].Second[x];
                    }
                }
            }

            //Инициализация маршрутов с доп. вершинами
            for (int g = 0; g < bus_count; g++)
            {
                List<int> stops = new List<int>();

                for (int i = 0, i2 = 0; i < all_stop_count; i++)
                {
                    i2 = 0;
                    if (!Array.Exists(graphs[g].stop, element => element == i)) continue;

                    int tmp = repeats.FindIndex(element => element.First == i);
                    if (tmp != -1 && repeats[tmp].Second[0] != g)
                    {
                        for (int z = 0; z < tmp; z++)
                        {
                            i2 += repeats[z].Second.Count - 1;
                        }
                        i2 += all_stop_count;
                        i2 += repeats[tmp].Second.FindIndex(element => element == g) - 1;
                    }
                    else
                    {
                        i2 = i;
                    }

                    for (int j = 0, j2 = 0; j < all_stop_count; j++)
                    {
                        j2 = 0;
                        if (!Array.Exists(graphs[g].stop, element => element == j) || graphs[g].matrx[i, j] == 0) continue;

                        int tmp2 = repeats.FindIndex(element => element.First == j);
                        if (tmp2 != -1 && repeats[tmp2].Second[0] != g)
                        {
                            for (int z = 0; z < tmp2; z++)
                            {
                                j2 += repeats[z].Second.Count - 1;
                            }
                            j2 += all_stop_count;
                            j2 += repeats[tmp2].Second.FindIndex(element => element == g) - 1;
                        }
                        else
                        {
                            j2 = j;
                        }

                        if (i2 != i && !Array.Exists(graphs2[g].stop, element => element == i2)) graphs2[g].stop[Array.FindIndex(graphs2[g].stop, element => element == i)] = i2;
                        if (j2 != j && !Array.Exists(graphs2[g].stop, element => element == j2)) graphs2[g].stop[Array.FindIndex(graphs2[g].stop, element => element == j)] = j2;

                        graphs2[g].matrx[i2, j2] = graphs[g].matrx[i, j];
                    }
                }
            }

            //Инициализация расширенного графа
            for (int g = 0; g < bus_count; g++)
            {
                for (int i = 0; i < ext_count; i++)
                {
                    for (int j = 0; j < ext_count; j++)
                    {
                        if (graphs2[g].matrx[i, j] > 0) graph_ext.matrx[i, j] = graphs2[g].matrx[i, j];
                    }
                }
            }

            for (int i = 0, iter = all_stop_count; i < repeats.Count; i++)
            {
                for (int j = 1; j < repeats[i].Second.Count; j++)
                {
                    graph_ext.matrx[repeats[i].First, iter] = graph_ext.matrx[iter, repeats[i].First] = -1; // Вес ребра неизвестен, будет просчитан во время алгоритма
                    iter++;
                }
            }


            // Список содержаший комбинации кратчайших путей от начальной точки до конечной
            List<Route> rg_routes = new List<Route>();

            for (int i = 0, st = 0; i < start_point_graphs.Count; i++)
            {
                st = 0;
                if (start_point_graphs.Count == 1) st = start_stop;
                else
                {
                    int start_idx = repeats.FindIndex(element => element.First == start_stop);
                    int start_bus_idx = repeats[start_idx].Second.FindIndex(element => element == start_point_graphs[i]);
                    if (start_bus_idx == 0) st = start_stop;
                    else
                    {
                        for (int z = 0; z < start_idx; z++)
                        {
                            st += repeats[z].Second.Count - 1;
                        }
                        st += all_stop_count;
                        st += repeats[start_idx].Second[start_bus_idx] - 1;
                    }
                }

                for (int j = 0, fn = 0; j < finish_point_graphs.Count; j++)
                {
                    fn = 0;
                    if (finish_point_graphs.Count == 1) fn = finish_stop;
                    else
                    {
                        int finish_idx = repeats.FindIndex(element => element.First == finish_stop);
                        int finish_bus_idx = repeats[finish_idx].Second.FindIndex(element => element == finish_point_graphs[i]);
                        if (finish_bus_idx == 0) fn = finish_stop;
                        else
                        {
                            for (int z = 0; z < finish_idx; z++)
                            {
                                fn += repeats[z].Second.Count - 1;
                            }
                            fn += all_stop_count;
                            fn += repeats[finish_idx].Second[finish_bus_idx] - 1;
                        }
                    }

                    rg_routes.Add(DijkstraModified(graph_ext, st, fn, st_time: NearestBus(start_point_graphs[i], start_stop, start_time), conv_bus: conv_bus, conv_stop: conv_stop));
                }
            }


            // Сортируем по весу (времени)
            rg_routes = rg_routes.OrderBy(x => x.Weight).ToList();

            if (rg_routes[0].Weight < MAX_TIME) // Если самый быстрый путь вышел за рамки, то ответа нет
            {
                // Конвертируем номера остановок обратно в старую нумерацию
                for (int i = 0; i < rg_routes[0].arr.Count; i++)
                {
                    int stop = rg_routes[0].arr[i].Stop;
                    rg_routes[0].arr[i] = new Route.Node(conv_bus[stop], conv_stop[stop]);
                }

                result[0] = rg_routes[0];
            }
        }

        private static void FindCheapestRoute()
        {
            // Граф, вершины которого являются маршрутами, с весами в вершинах в виде стоимости
            Graph routes_graph = new Graph();
            routes_graph.stop_count = bus_count;
            routes_graph.matrx = new int[bus_count, bus_count];

            // Матрица пересечении маршрутов. Элемент содержит список остановок, которые пересекаются в маршрутах i, j. Значение int - итератор
            Pair<int, List<int>>[,] intersections = new Pair<int, List<int>>[bus_count, bus_count];

            // Находим пересечения 
            for (int i = 0; i < bus_count - 1; i++)
            {
                for (int j = i + 1; j < bus_count; j++)
                {
                    intersections[i, j] = new Pair<int, List<int>>();
                    intersections[j, i] = intersections[i, j];
                    intersections[i, j].First = 0;
                    intersections[i, j].Second = GetIntersection(i, j);
                    if (intersections[i, j].Second.Count > 0)
                    {
                        routes_graph.matrx[i, j] = routes_graph.matrx[j, i] = 1;
                    }
                }
            }

            // Список содержаший комбинации путей от начальной точки до конечной
            List<Route> rg_routes = new List<Route>();

            foreach(int st in start_point_graphs)
            {
                foreach (int fn in finish_point_graphs)
                {
                    rg_routes.Add(DijkstraModified(routes_graph, st, fn, by_price: true));
                }
            }            

            // Сортируем по весу (стоимости)
            rg_routes = rg_routes.OrderBy(x => x.Weight).ToList();

            // "Раскрываем" каждый маршрут из маршрутов в маршрут из остановок, учитываю комбинации пересечений маршрутов
            foreach (Route elem in rg_routes)
            {
                int intersct_count = 1;
                bool increase_next = true;

                // Количество комбинаций всех пересечений пути равна произведению данных пересечений
                for (int i = 1; i < elem.arr.Count; i++) intersct_count *= intersections[elem.arr[i].Stop, elem.arr[i - 1].Stop].Second.Count;

                int[,] stops_route = new int[intersct_count, elem.arr.Count + 1];

                for (int i = 0; i < intersct_count; i++)
                {
                    stops_route[i, 0] = start_stop;
                    for (int j = 1; j < elem.arr.Count; j++)
                    {
                        if (intersections[elem.arr[j].Stop, elem.arr[j - 1].Stop].Second.Count > 1 && increase_next)
                        {
                            if (intersections[elem.arr[j].Stop, elem.arr[j - 1].Stop].First != intersections[elem.arr[j].Stop, elem.arr[j - 1].Stop].Second.Count - 1)
                            {
                                intersections[elem.arr[j].Stop, elem.arr[j - 1].Stop].First++;
                                increase_next = false;
                            }
                            else
                            {
                                intersections[elem.arr[j].Stop, elem.arr[j - 1].Stop].First = 0;
                                increase_next = true;
                            }
                        }
                        stops_route[i, j] = intersections[elem.arr[j].Stop, elem.arr[j - 1].Stop].Second[intersections[elem.arr[j].Stop, elem.arr[j - 1].Stop].First];  // ¯\_(ツ)_/¯                              
                    }
                    stops_route[i, elem.arr.Count] = finish_stop;

                    increase_next = true;

                }


                for (int i = 0; i < intersct_count; i++)
                {
                    Route result_route = new Route();

                    for (int j = 0; j < elem.arr.Count; j++)                    
                        result_route.Add(DijkstraModified(graphs[elem.arr[j].Stop], stops_route[i, j], stops_route[i, j + 1], bus_idx: elem.arr[j].Stop));                    

                    if (RouteTime(result_route) < MAX_TIME)
                    {
                        result[1] = result_route;
                        break;
                    }
                    else
                    {
                        if(RoutePrice(result[0]) == RoutePrice(result_route))
                        {
                            result[1] = result[0];
                            break;
                        }
                    }
                }

                if (result[1] != null) break;
            }
        }
          
        private static Route DijkstraModified(Graph gr, int start, int finish, int bus_idx = -1, int st_time = 0, int[] conv_bus = null, int[] conv_stop = null, bool by_price = false)
        {
            int gr_len = Convert.ToInt32(Math.Sqrt(gr.matrx.Length)); 
            int finish_idx = 0;
            bool[] visited = new bool[gr_len];
            bool is_undirect;
            List<Route> branch = new List<Route>();
            int[] weights = new int[gr_len];
            for (int x = 0; x < gr_len; x++) weights[x] = int.MaxValue;

            branch.Add(new Route());
            branch[0].arr.Add(new Route.Node(bus_idx, start));
            if (!by_price)
            {
                branch[0].Weight = (conv_bus == null) ? 0 : NearestBus(conv_bus[start], conv_stop[start], st_time);
                weights[start] = branch[0].Weight;
            }
            else
                branch[0].Weight = weights[start] = price[start];

            bool end = false;
            while (!end)
            {
                end = true;

                int r_count = branch.Count; // Чтобы после каждой итерации не пересчитывался routes.Count
                for (int k = 0; k < r_count; k++)
                {
                    if (branch[k].arr.Last().Stop != finish)
                    {
                        for (int l = 0; l < gr_len; l++)
                        {
                            int tmp_sum = (conv_bus == null && !by_price) ? weights[branch[k].arr.Last().Stop] : branch[k].Weight;

                            if (!by_price)
                            {
                                if (gr.matrx[branch[k].arr.Last().Stop, l] != -1)
                                    tmp_sum += gr.matrx[branch[k].arr.Last().Stop, l];
                                else
                                    tmp_sum = NearestBus(conv_bus[l], conv_stop[l], tmp_sum);
                            }
                            else
                            {
                                tmp_sum += price[l];
                            }

                            is_undirect = (!by_price && gr.matrx[branch[k].arr.Last().Stop, l] != -1) ? true : !visited[l];

                            if (gr.matrx[branch[k].arr.Last().Stop, l] != 0 && is_undirect &&  tmp_sum < weights[l])
                            {
                                end = false;

                                Route tmp = branch[k].Copy();
                                tmp.arr.Add(new Route.Node(bus_idx, l));

                                if (!by_price)
                                {
                                    weights[l] = tmp_sum;
                                    tmp.Weight = (conv_bus == null) ? 0 : tmp_sum;
                                }
                                else
                                {
                                    tmp.Weight += price[l];
                                    weights[l] = tmp.Weight;
                                }                                

                                branch.Add(tmp);
                            }
                        }

                        visited[branch[k].arr.Last().Stop] = true;

                        branch.RemoveAt(k);
                        k--;
                        r_count--;
                    }
                    else
                    {
                        finish_idx = k;
                    }
                }

            }

            return (branch.Count > finish_idx) ? branch[finish_idx] : null;
        }

        private static List<int> GetIntersection(int gr1, int gr2)
        {
            List<int> intr = new List<int>();

            for (int i = 0; i < graphs[gr1].stop_count; i++)
            {
                if (Array.Exists(graphs[gr2].stop, element => element == graphs[gr1].stop[i]))
                {
                    intr.Add(graphs[gr1].stop[i]);
                }
            }

            return intr;
        }

        private static int RouteTime(Route x, List<int> log = null)  // Подсчет времени маршрута с учетом начала работы и времени отправления
        {
            if (log == null) log = new List<int>();  
            int result = NearestBus(x.arr[0].Bus, x.arr[0].Stop, start_time);
            log.Add(result);

            if (x.arr.Count > 1)
            {
                for (int i = 1; i < x.arr.Count; i++)
                {
                    if (x.arr[i].Bus != x.arr[i - 1].Bus)
                    {
                        result = NearestBus(x.arr[i].Bus, x.arr[i].Stop, result); ;
                        log.Add(result);
                    }
                    else
                    {
                        result += graphs[x.arr[i].Bus].matrx[x.arr[i - 1].Stop, x.arr[i].Stop];
                        log.Add(result);
                    }
                }
            }

            return result;
        }

        private static int NearestBus(int bus, int stop, int start) // Сколько ждать автобус
        {
            int arrival = work_time[bus];
            for (int i = 0; i < graphs[bus].stop_count; i++)
            {
                if (stop == graphs[bus].stop[i]) break;
                else arrival += graphs[bus].matrx[graphs[bus].stop[i], graphs[bus].stop[i + 1]];
            }

            while (arrival < start)
                arrival += graphs[bus].full_circle;

            return arrival;
        }

        private static int RoutePrice(Route x)
        {
            int last = x.arr[0].Bus;
            int sum = price[last];

            for (int i = 1; i < x.arr.Count; i++)
            {
                if(last != x.arr[i].Bus)
                {
                    last = x.arr[i].Bus;
                    sum += price[last];
                }
            }

            return sum;
        }
    }
}
