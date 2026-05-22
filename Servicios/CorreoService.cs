using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SistemaBiometricoUMG.Servicios
{
    public class CorreoService
    {
        private readonly IConfiguration _config;

        public CorreoService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarCarnet(string destinatario,
                                       string nombrePersona,
                                       byte[] pdfBytes)
        {
            var conf = _config.GetSection("ConfigCorreo");

            var mensaje = new MimeMessage();

            // Remitente
            mensaje.From.Add(new MailboxAddress(
                conf["NombreRemitente"],
                conf["CorreoRemitente"]
            ));

            // Destinatario
            mensaje.To.Add(new MailboxAddress(nombrePersona, destinatario));

            // Asunto
            mensaje.Subject = "🎓 Tu Carnet de Identificación UMG";

            // Cuerpo del correo
            var builder = new BodyBuilder();

            builder.HtmlBody = $@"
                <div style='font-family:Arial; max-width:600px; margin:auto;'>
                    <div style='background-color:#003366; padding:20px; text-align:center;'>
                        <h2 style='color:white; margin:0;'>
                            Universidad Mariano Gálvez
                        </h2>
                        <p style='color:#ccc; margin:5px 0;'>
                            Sede Boca del Monte
                        </p>
                    </div>
                    <div style='padding:30px; background:#f9f9f9;'>
                        <h3>Hola, {nombrePersona} 👋</h3>
                        <p>
                            Tu registro en el sistema biométrico UMG fue completado 
                            exitosamente.
                        </p>
                        <p>
                            Adjunto encontrarás tu <strong>carnet de identificación</strong> 
                            en formato PDF. Por favor guárdalo y preséntalo cuando 
                            sea requerido.
                        </p>
                        <div style='background:#003366; color:white; padding:15px; 
                                    border-radius:8px; text-align:center; margin:20px 0;'>
                            <strong>Sistema Biométrico de Control de Acceso</strong><br/>
                            Universidad Mariano Gálvez
                        </div>
                        <p style='color:#999; font-size:12px;'>
                            Este es un correo automático, por favor no respondas.
                        </p>
                    </div>
                </div>
            ";

            // Adjuntar el PDF
            builder.Attachments.Add(
                $"Carnet_UMG.pdf",
                pdfBytes,
                new ContentType("application", "pdf")
            );

            mensaje.Body = builder.ToMessageBody();

            // Enviar
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                conf["Servidor"],
                int.Parse(conf["Puerto"]),
                SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(
                conf["CorreoRemitente"],
                conf["Contrasena"]
            );
            await smtp.SendAsync(mensaje);
            await smtp.DisconnectAsync(true);
        }
    }
}
