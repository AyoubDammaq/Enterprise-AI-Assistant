
1. Tool Success:

User
 ↓
LLM
 ↓
Tool Call
 ↓
Validation ✅
 ↓
Tool Execution ✅
 ↓
Tool Result
 ↓
LLM
 ↓
Final Response


2. Tool Invalid Input:

Le LLM demande quelque chose d'incorrect.

LLM
 ↓
Tool Call
 ↓
Validation ❌
 ↓
Invalid Input
 ↓
LLM
 ↓
User-friendly response


3. Tool Business Error:

Les paramètres sont techniquement valides, mais l'opération n'est pas autorisée par les règles métier.

Validation ✅
Business Rule ❌


4. Tool Technical Error:

Tool
 ↓
SQL Server
 ↓
Connection failed ❌

Le Tool ne doit pas transformer silencieusement ce problème en succès.
