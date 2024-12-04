using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Protocolo;

namespace Servidor
{
    class Servidor
    {
        private static TcpListener escuchador; // Listener para aceptar conexiones
        private static Protocolo.Protocolo protocolo = new Protocolo.Protocolo(); // Instancia del Protocolo
        private static Dictionary<string, int> listadoClientes = new Dictionary<string, int>(); // Almacena las solicitudes por cliente

        static void Main(string[] args)
        {
            try
            {
                // Configura el servidor para escuchar en el puerto 8080
                escuchador = new TcpListener(IPAddress.Any, 8080);
                escuchador.Start();
                Console.WriteLine("Servidor iniciado en el puerto 8080...");

                while (true)
                {
                    // Acepta una conexión entrante
                    TcpClient cliente = escuchador.AcceptTcpClient();
                    Console.WriteLine($"Cliente conectado desde: {cliente.Client.RemoteEndPoint}");
                    // Crea un hilo para manejar al cliente
                    Thread hiloCliente = new Thread(ManipuladorCliente);
                    hiloCliente.Start(cliente);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error de socket: {ex.Message}");
            }
            finally
            {
                escuchador?.Stop(); // Detiene el listener en caso de error
            }
        }

        private static void ManipuladorCliente(object obj)
        {
            TcpClient cliente = (TcpClient)obj; // Convierte el objeto en TcpClient
            NetworkStream flujo = null; // Flujo para comunicación con el cliente

            try
            {
                flujo = cliente.GetStream(); // Obtiene el flujo del cliente
                byte[] bufferRx = new byte[1024]; // Buffer para recibir datos
                int bytesRx;

                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0)
                {
                    // Convierte los bytes recibidos a texto
                    string mensajeRx = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);
                    Console.WriteLine($"Mensaje recibido: {mensajeRx}");

                    // Procesa el mensaje como un Pedido
                    Pedido pedido = Pedido.Procesar(mensajeRx);
                    string direccionCliente = cliente.Client.RemoteEndPoint.ToString();

                    // Resuelve el pedido utilizando el protocolo
                    Respuesta respuesta = protocolo.ResolverPedido(pedido, direccionCliente, listadoClientes);
                    Console.WriteLine($"Respuesta enviada: {respuesta}");

                    // Envía la respuesta al cliente
                    byte[] bufferTx = Encoding.UTF8.GetBytes(respuesta.ToString());
                    flujo.Write(bufferTx, 0, bufferTx.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al manejar cliente: {ex.Message}");
            }
            finally
            {
                flujo?.Close(); // Cierra el flujo
                cliente?.Close(); // Cierra la conexión del cliente
            }
        }
    }
}

