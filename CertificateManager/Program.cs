using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace CertificateManager
{
    class Program
    {
        private const string CerName = "GestureSignCA.cer";

        private static void Main(string[] args)
        {
            if (!File.Exists(CerName)) return;
            X509Certificate2 certificate = new X509Certificate2(CerName);
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser); try
            {
                store.Open(OpenFlags.ReadWrite);
                if (args.Length == 0)
                {
                    if (store.Certificates.Contains(certificate))
                        store.Remove(certificate);
                    else
                        store.Add(certificate);
                }
                else if (args[0].Equals("/I", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!store.Certificates.Contains(certificate))
                        store.Add(certificate);
                }
                else if (args[0].Equals("/U", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (store.Certificates.Contains(certificate))
                        store.Remove(certificate);
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                store.Close();
            }
        }
    }
}
