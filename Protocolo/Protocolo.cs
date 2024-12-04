using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Protocolo
{
    // Clase para representar un pedido
    public class Pedido
    {
        public string Comando { get; set; } // Comando principal del pedido
        public string[] Parametros { get; set; } // Parámetros adicionales del pedido

        // Método para procesar un mensaje recibido y convertirlo en un objeto Pedido
        public static Pedido Procesar(string mensaje)
        {
            var partes = mensaje.Split(' '); // Divide el mensaje en partes separadas por espacios
            return new Pedido
            {
                Comando = partes[0].ToUpper(), // El primer elemento es el comando (en mayúsculas)
                Parametros = partes.Skip(1).ToArray() // Los elementos restantes son los parámetros
            };
        }

        // Representación del pedido como cadena de texto
        public override string ToString()
        {
            return $"{Comando} {string.Join(" ", Parametros)}"; // Junta el comando y los parámetros
        }
    }

    // Clase para representar una respuesta
    public class Respuesta
    {
        public string Estado { get; set; } // Estado de la respuesta (ej. OK o NOK)
        public string Mensaje { get; set; } // Mensaje detallado de la respuesta

        // Representación de la respuesta como cadena de texto
        public override string ToString()
        {
            return $"{Estado} {Mensaje}";
        }
    }

    // Clase principal del protocolo
    public class Protocolo
    {
        // Método para enviar un pedido al servidor y procesar la respuesta
        public Respuesta HazOperacion(Pedido pedido, NetworkStream flujo)
        {
            if (flujo == null)
            {
                throw new InvalidOperationException("No hay conexión disponible."); // Verifica que exista conexión
            }

            // Convierte el pedido en bytes para enviarlo por el flujo
            byte[] bufferTx = Encoding.UTF8.GetBytes(pedido.ToString());
            flujo.Write(bufferTx, 0, bufferTx.Length); // Escribe los datos en el flujo de salida

            // Prepara un buffer para recibir la respuesta
            byte[] bufferRx = new byte[1024];
            int bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length); // Lee los datos recibidos

            // Convierte los bytes recibidos en una cadena de texto
            string mensaje = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);
            var partes = mensaje.Split(' '); // Divide la respuesta en partes

            // Retorna un objeto Respuesta basado en los datos recibidos
            return new Respuesta
            {
                Estado = partes[0], // El primer elemento es el estado
                Mensaje = string.Join(" ", partes.Skip(1).ToArray()) // El resto es el mensaje
            };
        }

        // Método para procesar un pedido y generar una respuesta
        public Respuesta ResolverPedido(Pedido pedido, string direccionCliente, Dictionary<string, int> listadoClientes)
        {
            // Respuesta predeterminada si el comando no es reconocido
            Respuesta respuesta = new Respuesta { Estado = "NOK", Mensaje = "Comando no reconocido" };

            // Evalúa el comando recibido
            switch (pedido.Comando)
            {
                case "INGRESO":
                    respuesta = ResolverIngreso(pedido); // Procesa un pedido de ingreso
                    break;
                case "CALCULO":
                    respuesta = ResolverCalculo(pedido, direccionCliente, listadoClientes); // Procesa un pedido de cálculo
                    break;
                case "CONTADOR":
                    respuesta = ResolverContador(direccionCliente, listadoClientes); // Procesa un pedido de contador
                    break;
            }

            return respuesta; // Retorna la respuesta generada
        }

        // Procesa un pedido de ingreso para validar credenciales
        private Respuesta ResolverIngreso(Pedido pedido)
        {
            // Verifica las credenciales
            if (pedido.Parametros.Length == 2 && pedido.Parametros[0] == "root" && pedido.Parametros[1] == "admin20")
            {
                // Aleatoriamente permite o niega el acceso
                return new Random().Next(2) == 0
                    ? new Respuesta { Estado = "OK", Mensaje = "ACCESO_CONCEDIDO" }
                    : new Respuesta { Estado = "NOK", Mensaje = "ACCESO_NEGADO" };
            }
            return new Respuesta { Estado = "NOK", Mensaje = "ACCESO_NEGADO" }; // Credenciales inválidas
        }

        // Procesa un pedido de cálculo basado en una placa
        private Respuesta ResolverCalculo(Pedido pedido, string direccionCliente, Dictionary<string, int> listadoClientes)
        {
            string placa = pedido.Parametros[2]; // Obtiene la placa del pedido
            if (Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$")) // Valida el formato de la placa
            {
                byte indicadorDia = ObtenerIndicadorDia(placa); // Obtiene el indicador del día según la placa
                IncrementarContadorCliente(direccionCliente, listadoClientes); // Incrementa el contador del cliente
                return new Respuesta { Estado = "OK", Mensaje = $"{placa} {indicadorDia}" }; // Retorna la respuesta
            }
            return new Respuesta { Estado = "NOK", Mensaje = "Placa no válida" }; // Respuesta si la placa es inválida
        }

        // Procesa un pedido para obtener el número de solicitudes realizadas por un cliente
        private Respuesta ResolverContador(string direccionCliente, Dictionary<string, int> listadoClientes)
        {
            return listadoClientes.ContainsKey(direccionCliente)
                ? new Respuesta { Estado = "OK", Mensaje = listadoClientes[direccionCliente].ToString() }
                : new Respuesta { Estado = "NOK", Mensaje = "No hay solicitudes previas" };
        }

        // Obtiene el indicador del día basado en el último dígito de la placa
        private byte ObtenerIndicadorDia(string placa)
        {
            int ultimoDigito = int.Parse(placa[placa.Length - 1].ToString()); // Obtiene el último dígito de la placa

            // Asocia el último dígito con un día de la semana
            switch (ultimoDigito)
            {
                case 1:
                case 2:
                    return 0b00100000; // Bytes para Lunes
                case 3:
                case 4:
                    return 0b00010000; // Bytes para Martes
                case 5:
                case 6:
                    return 0b00001000; // Bytes para Miércoles
                case 7:
                case 8:
                    return 0b00000100; // Bytes para Jueves
                case 9:
                case 0:
                    return 0b00000010; // Bytes para Viernes
                default:
                    return 0; // Valor predeterminado
            }
        }

        // Incrementa el contador de solicitudes realizadas por un cliente
        private void IncrementarContadorCliente(string direccionCliente, Dictionary<string, int> listadoClientes)
        {
            if (listadoClientes.ContainsKey(direccionCliente))
                listadoClientes[direccionCliente]++; // Incrementa el contador si el cliente ya existe
            else
                listadoClientes[direccionCliente] = 1; // Inicializa el contador si es un cliente nuevo
        }
    }
}
