using System;
using System.Windows.Forms;
using System.Net.Sockets;
using Protocolo;

namespace Cliente
{
    public partial class FrmValidador : Form
    {
        private TcpClient remoto; // Cliente TCP que se conecta al servidor
        private NetworkStream flujo; // Flujo de red para enviar y recibir datos
        private Protocolo.Protocolo protocolo; // Instancia del protocolo para manejar pedidos y respuestas

        public FrmValidador()
        {
            InitializeComponent(); // Inicializa los componentes del formulario
            protocolo = new Protocolo.Protocolo(); // Inicializa la instancia del protocolo
        }

        private void FrmValidador_Load(object sender, EventArgs e)
        {
            try
            {
                // Intenta conectarse al servidor en localhost (127.0.0.1) en el puerto 8080
                remoto = new TcpClient("127.0.0.1", 8080);
                flujo = remoto.GetStream(); // Obtiene el flujo para la comunicación con el servidor
            }
            catch (SocketException ex)
            {
                // Si ocurre un error de conexión, muestra un mensaje y cierra recursos
                MessageBox.Show("No se pudo establecer conexión: " + ex.Message, "ERROR");
                flujo?.Close(); // Cierra el flujo si fue inicializado
                remoto?.Close(); // Cierra el cliente TCP si fue inicializado
            }

            // Deshabilita los controles relacionados con las operaciones hasta que el usuario inicie sesión correctamente
            panPlaca.Enabled = false;
            chkLunes.Enabled = chkMartes.Enabled = chkMiercoles.Enabled =
                chkJueves.Enabled = chkViernes.Enabled = chkSabado.Enabled =
                chkDomingo.Enabled = false;
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            // Obtiene el usuario y contraseña ingresados por el usuario
            string usuario = txtUsuario.Text;
            string contraseña = txtPassword.Text;

            // Valida que ambos campos no estén vacíos
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contraseña))
            {
                MessageBox.Show("Se requiere el ingreso de usuario y contraseña.", "ADVERTENCIA");
                return;
            }

            // Crea un pedido con el comando "INGRESO" y los parámetros del usuario y contraseña
            Pedido pedido = new Pedido
            {
                Comando = "INGRESO",
                Parametros = new[] { usuario, contraseña }
            };

            // Envía el pedido al servidor utilizando el protocolo y recibe la respuesta
            Respuesta respuesta = protocolo.HazOperacion(pedido, flujo);

            if (respuesta == null)
            {
                // Si no se recibe una respuesta, muestra un mensaje de error
                MessageBox.Show("Hubo un error al realizar la operación.", "ERROR");
                return;
            }

            // Procesa la respuesta del servidor
            if (respuesta.Estado == "OK" && respuesta.Mensaje == "ACCESO_CONCEDIDO")
            {
                // Si las credenciales son correctas, habilita el panel de operaciones y deshabilita el inicio de sesión
                panPlaca.Enabled = true;
                panLogin.Enabled = false;
                MessageBox.Show("Acceso concedido.", "INFORMACIÓN");
            }
            else
            {
                // Si las credenciales son incorrectas, deshabilita el panel de operaciones y habilita el inicio de sesión
                panPlaca.Enabled = false;
                panLogin.Enabled = true;
                MessageBox.Show("Acceso denegado. Verifique sus credenciales.", "ERROR");
            }
        }

        private void btnConsultar_Click(object sender, EventArgs e)
        {
            // Obtiene los datos ingresados en los campos de modelo, marca y placa
            string modelo = txtModelo.Text;
            string marca = txtMarca.Text;
            string placa = txtPlaca.Text;

            // Crea un pedido con el comando "CALCULO" y los parámetros ingresados
            Pedido pedido = new Pedido
            {
                Comando = "CALCULO",
                Parametros = new[] { modelo, marca, placa }
            };

            // Envía el pedido al servidor utilizando el protocolo y recibe la respuesta
            Respuesta respuesta = protocolo.HazOperacion(pedido, flujo);

            if (respuesta == null)
            {
                // Si no se recibe una respuesta, muestra un mensaje de error
                MessageBox.Show("Hubo un error en la solicitud.", "ERROR");
                return;
            }

            // Procesa la respuesta recibida
            if (respuesta.Estado == "NOK")
            {
                // Si el servidor devuelve un error, muestra un mensaje
                MessageBox.Show("Error en la solicitud.", "ERROR");
            }
            else
            {
                // Si la respuesta es válida, procesa los días permitidos para la placa
                ProcesarDias(respuesta.Mensaje);
            }
        }

        private void btnNumConsultas_Click(object sender, EventArgs e)
        {
            // Crea un pedido con el comando "CONTADOR" sin parámetros
            Pedido pedido = new Pedido
            {
                Comando = "CONTADOR",
                Parametros = Array.Empty<string>()
            };

            // Envía el pedido al servidor utilizando el protocolo y recibe la respuesta
            Respuesta respuesta = protocolo.HazOperacion(pedido, flujo);

            if (respuesta == null)
            {
                // Si no se recibe una respuesta, muestra un mensaje de error
                MessageBox.Show("Hubo un error en la solicitud.", "ERROR");
                return;
            }

            // Muestra el número de consultas realizadas por el cliente
            MessageBox.Show("Número de consultas: " + respuesta.Mensaje, "INFORMACIÓN");
        }

        private void ProcesarDias(string mensaje)
        {
            // Divide la respuesta en partes y valida que contenga el formato esperado
            var partes = mensaje.Split(' ');
            if (partes.Length < 2 || !byte.TryParse(partes[1], out byte resultado))
            {
                MessageBox.Show("Respuesta inválida.", "ERROR");
                return;
            }

            // Marca los días correspondientes al indicador recibido
            chkLunes.Checked = (resultado & 0b00100000) != 0;
            chkMartes.Checked = (resultado & 0b00010000) != 0;
            chkMiercoles.Checked = (resultado & 0b00001000) != 0;
            chkJueves.Checked = (resultado & 0b00000100) != 0;
            chkViernes.Checked = (resultado & 0b00000010) != 0;
        }

        private void FrmValidador_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cierra los recursos de red (flujo y cliente TCP) cuando el formulario se cierra
            flujo?.Close();
            remoto?.Close();
        }
    }
}

