using System.Threading.Tasks;

namespace TeamsWP.Inlays
{
  interface IInlay
  {
    Task Refresh();
    void Flush();
  }
}