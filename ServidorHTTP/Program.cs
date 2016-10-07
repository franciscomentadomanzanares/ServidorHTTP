using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Creamos el pool de hilos
/// Enlace con una IP y un puerto
/// Cuando recibamos datos, aceptamos la conexión
/// Procesamos la conexión en el pool de hilos
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

            /// <summary>
            /// Variables de bloqueo del recurso compartido del hilo (método)
            /// </summary>
            private static object _lockHilo = new object();
            private static bool _exitHilo;

            /// <summary>
            /// Variables de bloqueo del recurso compartido de la lista de clientes conectados.
            /// </summary>
            private static object _lockObtenerCliente = new object();
            private static bool _exitObtenerCliente;
            private static object _lockGuardarCliente = new object();
            private static bool _exitGuardarCliente;


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

            /// <summary>
            /// Asignamos los clientes conectados a la lista
            /// </summary>
            static class ClientesConectados
            {
                /// <summary>
                /// Lista de clientes
                /// </summary>
                static private List<TcpClient> clientesConectados = new List<TcpClient>();

                /// <summary>
                /// //Obtenemos un cliente de la lista
                /// </summary>
                /// <remarks>En caso de no haber clientes en lista o estar la lista bloqueada devuelve nulo</remarks>
                static public TcpClient ObtenerCliente
                {
                    get
                    {
                        lock (_lockObtenerCliente)
                        {
                            if (!_exitObtenerCliente)
                            {
                                _exitObtenerCliente = true;
                                var cliente = clientesConectados.FirstOrDefault();
                                //Si obtenemos un cliente lo eliminamos de la lista
                                if (cliente != null)
                                {
                                    clientesConectados.Remove(cliente);
                                }
                                _exitObtenerCliente = false;
                                return cliente;
                            }
                        }
                        return null;
                    }
                }
                /// <summary>
                /// Guardamos el cliente en la lista de clientes conectados
                /// </summary>
                /// <param name="clienteConectado">cliente que establece la conexión</param>
                static public void GuardarCliente(TcpClient clienteConectado)
                {
                    lock (_lockGuardarCliente)
                    {
                        if (!_exitGuardarCliente)
                        {
                            _exitGuardarCliente = true;
                            clientesConectados.Add(clienteConectado);
                            _exitGuardarCliente = false;
                        }
                    }
                }
            }

            /// <summary>
            /// Patrón singleton que genera los hilos que usa el Server
            /// </summary>
            private sealed class SingletonHilos
            {
                //Lista de hilos
                private static List<Thread> hilos = new List<Thread>();
                public static readonly SingletonHilos instancia = new SingletonHilos();

                /// <summary>
                /// Inicializamos los hilos
                /// </summary>
                /// <param name="numeroHilos"></param>
                public void Init(int numeroHilos)
                {
                    if (numeroHilos < 1)
                    {
                        throw new ArgumentOutOfRangeException("El número de hilos debe de ser un valor positivo");
                    }
                    for (var i = 0; i < numeroHilos; i++)
                    {
                        Thread hilo = new Thread(() => Hilo()) { IsBackground = true };
                        hilo.Name = $"Hilo-{Thread.CurrentThread.ManagedThreadId}";
                        hilos.Add(hilo);
                        hilo.Start();
                    }

                    var ids = hilos.Select(x => x.ManagedThreadId).ToArray();
                    Console.WriteLine($"núm hilos ={hilos.Count} Hilos id creados {string.Join(" - ", ids)}");
                }
            }


            /// <summary>
            /// Método ejecutado por cada hilo
            /// </summary>
            static void Hilo()
            {
                while (true)
                {
                    lock (_lockHilo)
                    {
                        if (!_exitHilo)
                        {
                            _exitHilo = true;
                            var cliente = ClientesConectados.ObtenerCliente;
                            if (cliente == null)
                            {
                                _exitHilo = false;
                                continue;
                            }
                            Console.WriteLine($"hilo={Thread.CurrentThread.ManagedThreadId} ");

                            _exitHilo = false;
                        }
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
                var asynResult = ar as IAsyncResult;
                TcpClient client = (asynResult.AsyncState as TcpListener).EndAcceptTcpClient(asynResult);
                ClientesConectados.GuardarCliente(client);
                client.Close();
            }
        }
    }
}
