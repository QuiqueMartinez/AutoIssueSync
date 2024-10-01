import { Octokit } from "@octokit/rest";
import fetch from 'node-fetch'; // Importar `node-fetch` para asignar `fetch`
import * as fs from 'fs';

// Configurar `fetch` en `Octokit`
const octokit = new Octokit({
  auth: process.env.TOKEN_GITHUB,
  request: {
    fetch: fetch // Asignar `node-fetch` como la implementación de `fetch`
  }
});

// Leer el archivo `issues.json`
const issuesFile = './issues.json';
if (!fs.existsSync(issuesFile)) {
  console.error("Error: No se encontró el archivo 'issues.json'");
  process.exit(1);
}

const issues = JSON.parse(fs.readFileSync(issuesFile, 'utf8'));

// Datos del repositorio
const owner = 'QuiqueMartinez';  // Cambia esto por el nombre de tu usuario u organización
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
