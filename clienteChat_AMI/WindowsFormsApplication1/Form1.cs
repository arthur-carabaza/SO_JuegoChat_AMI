﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        Socket server;
        Thread atender;

        bool conectado = false;

        string invitadoActual = null; // Guarda el nombre del invitado actual
        bool salaPreparada = false; // Indica si la sala está lista para iniciar
        int idSala = -1;
        bool EresInvitado = false;
        bool iniciadoSession = false;

        delegate void DelegadoParaEscribir(string mensaje);
        delegate void DelegadoParaActualizarLista(string mensaje);
        delegate void DelegadoParaMostrarMensaje(string mensaje);
        delegate void DelegadoParaActualizarNombrePartida(string nombre);
        delegate void DelegadoParaEscribirInitados(string Inombre);

        delegate void DelegadoParaAbrirBoton();
        delegate void DelegadoParaBorrar();

        List<FormChat> formularios = new List<FormChat> ();
        int[] IDSalas = new int[50];

        private void DeshabilitarBoton()
        {
            botonInciarSala.Enabled = false; // Deshabilitar botón
        }

        private void Borrar()
        {
            InvitarBox.Text = null; // Limpiar la caja de texto
        }


        private void ActualizarNombrePartida(string nombre)
        {
            NombrePartida.Text = nombre;
        }

        private int DameFormsPorID (int ID)
        {
            //ESTa FUNCION DEVULVE lA POSICION EN EL CUAL ESTA EL VECTOR DANDO SU ID DE SALA
            int i=0;
            int numforms = 0;
            while (i < IDSalas.Length)
            {
                if (ID == IDSalas[i])
                {
                    i = IDSalas.Length;
                }
                else
                    numforms++;

                if (numforms > IDSalas.Length)
                {
                    MessageBox.Show("Error");
                    break;
                }
                i++;
            }
            return numforms;
        }

        private void AbrirChat(int ID,string nombres)
        {
            int cont = formularios.Count;
            FormChat chatform = new FormChat(server,nombreTextBox.Text,ID,nombres);
            formularios.Add(chatform);
            IDSalas[cont] = (ID);
            chatform.ShowDialog();
            
        }

        private void ActualizarListaConectados(string mensaje)
        {
            ListaConectados.Text = mensaje;
        }
        private void EscribirInvitados(string Inombre)
        {
            labelInvitado.Text = Inombre;

        }

        private void MostrarMensaje(string mensaje)
        {
            MessageBox.Show(mensaje);
        }


        public Form1()
        {
            InitializeComponent();
                
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Establecer los colores iniciales de los botones
            conectar.BackColor = Color.Red; // Color rojo para el botón conectar
            conectar.ForeColor = Color.White;

            desconectar.BackColor = Color.Black; // Color negro para el botón desconectar
            desconectar.ForeColor = Color.White;
            this.Paint += new PaintEventHandler(Form1_Paint);
        }

        private void Form1_Paint(object senser, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Brush brush = conectado ? Brushes.Green : Brushes.Red; // Verde si está conectado, rojo si no
            g.FillEllipse(brush, new Rectangle(20,20,20,20)); // Dibujar el círculo en la esquina superior izquierda
        }

        private void AtenderServidor()
        {
            while (true)
            {
                //Recibimos mensaje del servidor
                byte[] msg2 = new byte[1024]; //Memoria ampliada de 1024 bytes
                int bytesRecibidos = server.Receive(msg2); 
                string mensajeCompleto = Encoding.ASCII.GetString(msg2, 0, bytesRecibidos); 
                string[] trozos = mensajeCompleto.Split('/');
                int codigo = Convert.ToInt32(trozos[0]);
                string mensaje; 
                string nombre = nombreTextBox.Text;

                //Nombre del formulario al que va destinado la respuesta


                switch (codigo)
                {
                    case 1: //Respuesta a iniciar sesión ; Se encarga el formulario principal 

                        //Aqui obtenemos el numero al cual va dirigido la respuesta
                        mensaje = trozos[2].Split('\0')[0];
                        if (trozos[1] == "SI")
                        {
                            this.Invoke(new DelegadoParaMostrarMensaje(MostrarMensaje), new object[] { mensaje });
                            this.Invoke(new DelegadoParaActualizarNombrePartida(ActualizarNombrePartida), new object[] { nombre });
                            iniciadoSession = true;

                        }
                        else if (trozos[1] == "NO")
                        {
                            this.Invoke(new DelegadoParaMostrarMensaje(MostrarMensaje), new object[] { mensaje });

                        }


                        break;


                    case 2: //Respuesta a registro ; Se encarga el formulario principal 

                        //Aqui obtenemos el numero al cual va dirigido la respuesta
                        mensaje = trozos[1].Split('\0')[0];

                        MessageBox.Show(mensaje);
                        break;

                    case 3: //RESULTADO QUERRY CON QUIEN HE ESTADO EN PARTIDA: 3/Juan,Carlos,Miguel
                        mensaje = trozos[1].Split('\0')[0];
                        MessageBox.Show(mensaje);
                        break;

                    case 4: //RESULTADO QUERRY CUANTOS MENSAJES HE ENVIADO A...: 4/4
                        mensaje = trozos[1].Split('\0')[0];
                        MessageBox.Show(mensaje);
                        break;

                    case 5: //RESULTADO QUERRY QUE SALAS SE HAN CREADO DESDE X tiempo : 5/ Sala1; Juan,Miguel | Sala2: Juan,Alvaro | Sala3: Alvaro,Pablo
                        mensaje = trozos[1].Split('\0')[0];
                        MessageBox.Show(mensaje);
                        break;

                    case 6: //Notificacion de conectados

                        //NO SE SI PONER AQUI LO DEL CODIGO, SE SUPONE QUE NO HAY QUE PONERLO
                        mensaje = trozos[1].Split('\0')[0];
                        this.Invoke(new DelegadoParaActualizarLista(ActualizarListaConectados), new object[] { mensaje }); 
                        break;

                    case 7: //Notificacion de invitacion

                        mensaje = trozos[1].Split('\0')[0];
                        DialogResult RespuestaInv = MessageBox.Show(mensaje + " le han invitado a una partida.\n Quiere unirse?", "Respuesta invitacion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (RespuestaInv == DialogResult.Yes)
                        {
                            EresInvitado = true;
                            string mensg = "8/0/"+mensaje+"/SI";
                            // Enviamos al servidor la respuesta de la invitacion
                            byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensg);
                            server.Send(msg);

                            
                        }
                        else
                        {
                            string mensg = "8/0/" + mensaje + "/NO";
                            // Enviamos al servidor la respuesta de la invitacion
                            byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensg);
                            server.Send(msg);
                        }
                        break;

                    case 8:

                        mensaje = trozos[1].Split('\0')[0];
                        if (mensaje == "SI")
                        {
                            MessageBox.Show("Su invitación ha sido aceptada");
                            idSala = Convert.ToInt32(trozos[2]); // Guardar ID de sala

                            string Ninvitado = InvitarBox.Text;
                            string Linvitados;
                            // Agregar el nombre del invitado a la lista
                            if (salaPreparada)
                            {
                                Linvitados = labelInvitado.Text + "/" + Ninvitado;
                                this.Invoke(new DelegadoParaEscribirInitados(EscribirInvitados), new object[] { Linvitados });
                            }
                            else
                            {
                                Linvitados = "Invitados: " + Ninvitado;
                                this.Invoke(new DelegadoParaEscribirInitados(EscribirInvitados), new object[] { Linvitados });
                            }

                            salaPreparada = true; // Marcar sala como lista
                            this.Invoke(new DelegadoParaBorrar(Borrar), new object[] {});
                        }
                        else
                        {
                            DialogResult result = MessageBox.Show("Su invitación ha sido rechazada, quiere poder iniciar igualmente la partida?","invitacion",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                            if (result == DialogResult.Yes)
                            {
                                salaPreparada = true; //Marcar sala como preparada
                                idSala = Convert.ToInt32(trozos[2]); // Guardar ID de sala

                            }
                            else
                            {
                                MessageBox.Show("La partida de cancela");
                                idSala = Convert.ToInt32(trozos[2]); // Guardar ID de sala
                                invitadoActual = null; // Resetear invitado
                                string invitados = "Invitado: Ninguno";
                                this.Invoke(new DelegadoParaEscribirInitados(EscribirInvitados), new object[] { invitados });
                                string mensg = "14/"+Convert.ToString(idSala);

                                this.Invoke(new DelegadoParaAbrirBoton(DeshabilitarBoton), new object[] { }); //Apagamos el boton
                                // Enviamos al servidor la respuesta de la invitacion
                                byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensg);
                                server.Send(msg);
                            }

                        }
                        break;

                    case 9:  //Manejar mensajes de creacion de partidas en general 
                        mensaje = trozos[1].Split('\0')[0];
                        this.Invoke(new DelegadoParaMostrarMensaje(MostrarMensaje), new object[] { mensaje }); 
                        break;

                    case 10: // Mensaje de chat recibido ; SE ENCARGAN FORMS SECUNDARIOS
                        //Aqui obtenemos el numero al cual va dirigido la respuesta
                        int IDsala = Convert.ToInt32(trozos[1]);
                        mensaje = trozos[2].Split('\0')[0];
                        int numforms = 0;

                        numforms = DameFormsPorID(IDsala);

                        

                        formularios[numforms].Recibir_Mensaje(mensaje);
                        break; 
                        

                    case 12: // Mensaje para iniciar sala
                        EresInvitado = false;
                        int salaID = Convert.ToInt32(trozos[1]);
                        string nombres = trozos[2];
                        ThreadStart ts = delegate { AbrirChat(salaID,nombres); };
                        Thread chatT = new Thread(ts);
                        chatT.Start();
                        break;

                    case 13:
                        //Aqui obtenemos el numero al cual va dirigido la respuesta
                        mensaje = trozos[1].Split('\0')[0];

                        MessageBox.Show(mensaje);
                        break;
                }
            }
        }
        private void conectar_Click(object sender, EventArgs e)
        {
            //Creamos un IPEndPoint con el ip del servidor y puerto del servidor 
            //al que deseamos conectarnos
            IPAddress direc = IPAddress.Parse("10.4.119.5");
            IPEndPoint ipep = new IPEndPoint(direc, 50000);


            //Creamos el socket 
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                server.Connect(ipep);//Intentamos conectar el socket

                conectado = true;
                this.Invalidate(new Rectangle(20,20,20,20)); //Redibujamos el forms para actiualizar el circulo 
                this.Update();

                MessageBox.Show("Conectado");
            }

            catch (SocketException )
            {
                //Si hay excepcion imprimimos error y salimos del programa con return 
                MessageBox.Show("No he podido conectar con el servidor");
                return;
            }

            //Poner en marcha el thread que atendera los mensajes del servidro
            ThreadStart ts = delegate { AtenderServidor(); };
            atender = new Thread(ts);
            atender.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if ((iniciadoSession)&&(conectado))
            {

                if (query3.Checked)
                {
                    //Quiere saber cuanto dinero tiene
                    string mensaje = "3/0/" + nombreTextBox.Text;
                    try
                    {
                        // Enviamos al servidor el nombre tecleado
                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
                        server.Send(msg);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al realizar la peticion, asegurese de estar conectado");
                    }
                }
                else if (query4.Checked)
                {
                    //Quiere saber cuantas victoras tengo
                    string mensaje = "4/0/" + nombreTextBox.Text + "/" + query.Text;
                    // Enviamos al servidor el nombre tecleado
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
                    server.Send(msg);
                }
                else
                {
                    //Quiere saber que cartas tengo
                    string mensaje = "5/0/" + nombreTextBox.Text + "/" + query.Text;
                    // Enviamos al servidor el nombre tecleado
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
                    server.Send(msg);
                }
            }

            else
            {
                MessageBox.Show("No has iniciado session o no estas conectado!");
            }
        }
        private void desconectar_Click(object sender, EventArgs e)
        {
            if (conectado)
            {
                //Mensaje de desconexion
                string mensaje = "0/";

                byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
                server.Send(msg);

                //Nos deconectamos
                conectado = false;
                iniciadoSession = false;
                atender.Abort();
                this.Invalidate(new Rectangle(20, 20, 40, 40)); //Redibujar el formulario para actualizar el circulo
                this.Update();

                ListaConectados.Text = null;
                NombrePartida.Text = null;
                nombreTextBox.Text = null;
                Password.Text = null;

                server.Shutdown(SocketShutdown.Both);
                server.Close();
            }
            else
            {
                MessageBox.Show("Ya estas desconectado!!!!");
            }
        }

        private void login_Click(object sender, EventArgs e)
        {
            if (conectado)
            {
                if (!iniciadoSession)
                {
                    //Quiere iniciar session
                    string mensaje = "1/0/" + nombreTextBox.Text + "/" + Password.Text;
                    // Enviamos al servidor el nombre tecleado y la contraseña
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
                    server.Send(msg);
                }
                else
                {
                    MessageBox.Show("Ya has iniciado session!!");
                }
            }
            else
            {
                MessageBox.Show("Conectate Primero!!!");
            }
        }
        private void registrarse_Click(object sender, EventArgs e)
        {
            if (conectado)
            {
                if ((nombreTextBox.Text == "") || (Password.Text == ""))
                {
                    MessageBox.Show("No has puesto la contraseña o el usuario ");
                }
                else
                {
                    //Quiere registrarse
                    string mensaje = "2/0/" + nombreTextBox.Text + "/" + Password.Text;
                    // Enviamos al servidor el nombre tecleado y la contraseña
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
                    server.Send(msg);
                }
            }
            else
            {
                MessageBox.Show("Conectate Primero!!!!!");
            }
        }
        private void label4_Click(object sender, EventArgs e)
        {
        }

        private void InvitarButton_Click(object sender, EventArgs e)
        {
            if ((iniciadoSession)&&(conectado))
            {
                if (!EresInvitado)
                {
                    if (InvitarBox.Text == nombreTextBox.Text)
                    {
                        MessageBox.Show("NO te puedes invitar a ti mismo!!!!");
                    }
                    else
                    {
                        string Ninvitado = InvitarBox.Text;
                        if (string.IsNullOrWhiteSpace(Ninvitado))
                        {
                            MessageBox.Show("Escriba un nombre de usuario que quiera invitar");
                        }
                        else
                        {
                            // Enviar mensaje de invitación al servidor
                            string mensaje = "7/0/" + InvitarBox.Text;
                            byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
                            server.Send(msg);

                            botonInciarSala.Enabled = true; // Habilitar el botón para empezar la partida
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Estas siendo invitado en una sala, espere a que el administrador de la sala acabe de crearla");
                }
                
            }
            else
            {
                MessageBox.Show("NO has iniciado session o no estas conectado");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Mensaje de desconexion
            string mensaje = "0/";

            byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
            server.Send(msg);

            //Nos deconectamos
            conectado = false;
            atender.Abort();
            this.Invalidate(new Rectangle(20, 20, 40, 40)); //Redibujar el formulario para actualizar el circulo
            this.Update();

            ListaConectados.Text = null;
            NombrePartida.Text = null;
            nombreTextBox.Text = null;
            Password.Text = null;

            server.Shutdown(SocketShutdown.Both);
            server.Close();
        }

        private void botonInciarSala_Click(object sender, EventArgs e)
        {
            if (salaPreparada && idSala != -1)
            {
                // Enviar mensaje al servidor para iniciar la sala
                string mensaje = "12/" + idSala;
                byte[] msg = Encoding.ASCII.GetBytes(mensaje);
                server.Send(msg);

                botonInciarSala.Enabled = false; // Desactivar el botón
                salaPreparada = false; // Resetear estado
                labelInvitado.Text = "Invitado: Ninguno";
            }
            else
            {
                MessageBox.Show("La sala no está lista para iniciar.");
            }
        }

        private void dardebaja_Click(object sender, EventArgs e)
        {
            if (conectado)
            {
                if (!iniciadoSession)
                {
                    //Quiere Darse de Baja
                    string mensaje = "13/0/" + nombreTextBox.Text;
                    // Enviamos al servidor el nombre tecleado y la contraseña
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(mensaje);
                    server.Send(msg);
                }
                else
                {
                    MessageBox.Show("Porfavor realiza la operacion de darse de baja sin estar haver inicado session");
                }
            }
            else
            {
                MessageBox.Show("Tienes que conectarte primero!");
            }
        }

        private void help_Click(object sender, EventArgs e)
        {
            FormHelp help = new FormHelp();
            help.ShowDialog(); 
        }
    }
}

namespace WindowsFormsApplication1
{
    static class Program
    {
        // Este es el punto de entrada principal para la aplicación.
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}



