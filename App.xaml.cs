using System.IO;
using System.Windows;

namespace Defectively.Horizon
{
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e) {
            try {

            } finally {
                base.OnExit(e);
            }
        }
    }
}
