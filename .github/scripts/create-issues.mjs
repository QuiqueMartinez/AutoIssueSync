// Cambiar `require` a `import` para ES Modules
import { Octokit } from "@octokit/rest";
import * as fs from 'fs';

// Leer el archivo `issues.json`
const issuesFile = './issues.json';
if (!fs.existsSync(issuesFile)) {
  console.error("Error: No se encontró el archivo 'issues.json'");
  process.exit(1);
}

const issues = JSON.parse(fs.readFileSync(issuesFile, 'utf8'));

// Obtener el token de GitHub desde las variables de entorno
const githubToken = process.env.TOKEN_GITHUB;
if (!githubToken) {
  console.error("Error: GITHUB_TOKEN no está configurado.");
  process.exit(1);
}

// Configurar el cliente de GitHub usando el token
const octokit = new Octokit({ auth: githubToken });

// Datos del repositorio
const owner = 'mi-usuario';  // Cambia esto por el nombre de tu usuario u organización
const repo = 'AutoIssueSync'; // Cambia esto por el nombre de tu repositorio

// Crear los issues en GitHub basados en `issues.json`
async function createIssues() {
  for (const issue of issues) {
    try {
      const response = await octokit.issues.create({
        owner: owner,
        repo: repo,
        title: issue.Title,
        body: `**Descripción**: ${issue.Description}\n**Tipo de Issue**: ${issue.IssueType}\n**Método Afectado**: ${issue.MethodName}\n**Archivo**: ${issue.FilePath}`,
        labels: [issue.IssueType] // Usar el tipo de issue como etiqueta
      });
      console.log(`Issue creado: ${response.data.html_url}`);
    } catch (error) {
      console.error(`Error al crear el issue '${issue.Title}': ${error.message}`);
    }
  }
}

createIssues();
