using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Enlace con una IP y un puerto
/// Cuando recibamos datos, aceptamos la conexión
/// Guardamos la conexión en el pool de hilos
/// Analizamos la cadena de datos
/// Creamos una respuesta a razón de la información obtenida anteriormente
/// Cerramos la conexión del cliente
/// </summary>

namespace ServidorHTTP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    namespace TCP_ListenerExample
    {
        class Program
        {
            private const int MAXIMO_HILOS = 10;
            private static object _lock = new object();
            private static bool _exit;


            private static object exitCall_lock = new object();
            static bool exitCall;
            public static bool ExitCall
            {
                get
                {
                    lock (exitCall_lock)
                    {
                        return exitCall;
                    }
                }
                set
                {
                    lock (exitCall_lock)
                    {
                        exitCall = value;
                    }
                }
            }

            ////Uso del patrón singleton
            public sealed class SingletonHilos
            {
                private static List<Thread> tareas = new List<Thread>();
                public static readonly SingletonHilos instancia = new SingletonHilos();

                public void Init(int numeroHilos)
                {
                    if (numeroHilos < 1)
                    {
                        throw new ArgumentOutOfRangeException("El número de hilos debe de ser un valor positivo");
                    }
                    for (var i = 0; i < numeroHilos; i++)
                    {
                        Thread tarea = new Thread(()=>Hilo());
                        tareas.Add(tarea);
                    }
                }
                public int NumeroHilosEnLista()
                {
                    return tareas.Count;
                }

                public void EjecutarHiloDisponible()
                {
                    var tarea = tareas.FirstOrDefault(x => !(x.IsAlive));
                    Console.WriteLine($"Activo ={tarea.IsAlive} id={tarea.ManagedThreadId}");
                    tarea.Start();
                    Console.WriteLine($"Activo ={tarea.IsAlive}");
                }
            }


            //Recurso compartido
            static void Hilo()
            {
                lock (_lock)
                {
                    if (!_exit)
                    {
                        _exit = true;
                        Console.WriteLine($"Activo ={Thread.CurrentThread.IsAlive}");
                    }
                }
            }
            /// <summary>
            /// Enlace Ip y puerto remoto
            /// Aceptamos la conexión
            /// </summary>
            /// <param name="args"></param>
            static void Main(string[] args)
            {
                SingletonHilos.instancia.Init(MAXIMO_HILOS);
                TcpListener listener = new TcpListener(IPAddress.Loopback, 8080);
                listener.Start();
                while (!ExitCall)
                {
                    listener.BeginAcceptTcpClient(ClientAccepted, listener);
                    Thread.Sleep(100);
                }
            }
            /// <summary>
            /// Guardamos la conexión en un pool de hilos
            /// </summary>
            /// <param name="ar"></param>
            static void ClientAccepted(IAsyncResult ar)
            {
                Console.WriteLine($"tareas = {SingletonHilos.instancia.NumeroHilosEnLista()}");
                var asynResult = ar as IAsyncResult;
                TcpClient client = (asynResult.AsyncState as TcpListener).EndAcceptTcpClient(asynResult); //Obtengo al cliente

                client.Close();
            }
        }
    }
}
