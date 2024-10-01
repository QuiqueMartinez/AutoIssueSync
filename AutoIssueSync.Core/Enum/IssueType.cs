using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoIssueSync.Core
{
    public enum IssueType
    {
        TASK,       // Tareas generales por completar
        BUG,        // Problemas a resolver, prioridad alta
        SMELL,      // Código que requiere refactorización o mejora
        ISSUE,      // Issue general, no clasificado como Bug o TASK
        FEATURE     // Solicitudes de características nuevas
    }
}
