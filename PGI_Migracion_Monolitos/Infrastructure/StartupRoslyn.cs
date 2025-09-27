using Microsoft.Build.Locator;

namespace PGI_Migracion_Monolitos.Infrastructure
{
    public static class StartupRoslyn
    {
        private static bool _registrado = false;

        public static void RegistrarMSBuild()
        {
            if (!_registrado && !MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
                _registrado = true;
                //Console.WriteLine("MSBuildLocator registrado correctamente.");
            }
        }
    }
}