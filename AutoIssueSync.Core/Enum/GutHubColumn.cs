using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoIssueSync.Core
{
    public enum GitHubColumn
    {
        TODO,          // Issues pendientes, por hacer
        IN_PROGRESS,   // Issues en los que se está trabajando actualmente
        REVIEW,        // Issues que están en revisión de código o calidad
        DONE           // Issues completados o resueltos
    }
}
