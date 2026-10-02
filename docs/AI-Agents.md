* Qu'est-ce qu'un AI Agent ?

Un AI Agent est un système capable d'effectuer des tâches d'un façon autonome, il peut prendre sa propre décision en fonction de ses connaissances et le contexte de travail. Il peut être déclencher à partir d'un prompt ou en arrière plan selon le cas d'utilisation. L'AI Agent suivie le framework ReAct alors il résoudre le problème dans son cerveau puis il fait l'action.

* Quelle différence entre une réponse LLM et une action ?

Un LLM est un système AI capable de générer des réponses textuelles, répondre aux questions et l'AI Agent est un système fait pour effectuer des tâches, prendre des décisions, accéder aux outils externes et raffiner ces retours d'une façon autonome.
Le LLM a une mémoire instantannée ou par session alors il ne peut pas le context auparavant si on change la session comme le basculement d'un chat à un autre dans ChatGPT par contre l'AI Agent peut concevoir le contexte où il travaille et il est adaptable au fil du temps donc il peut savoir les résultats et les interactions du passés pour aider à prendre la meilleure décision chaque fois.

* Quelle différence entre Tool Calling et Agentic Loop ?

Le Tool Calling utilise LLM pour appeler des outils externes et retourner des réponses qu'on peut les ajuster et l'Agentic Loop est un process autonome où l'agent fait appeler des outils externes selon une planification pour achever une tâche en s'adaptant aux résultats de chaque étapes pour les corriger ou l'utiliser dans les autres étapes.

* Pourquoi un Agent a-t-il besoin d'un objectif ?

Parce que l'agent utilise cet objectif pour raisonner et planfier les étapes à suivre et pour être adaptable au résultat de chaque étape en fonction de l'objectif fixé dés le début.

* Pourquoi un Agent peut-il avoir besoin de plusieurs étapes ?

Parce qu'il a besoin de faire un raisonnement et apprentissage avant l'exécution des tâches et dans le cas où il a besoin d'un outil interne il suivie un process de plusieurs étapes puis il utilise le résultat pour prendre la décision.


* Comprendre la boucle fondamentale d'un Agent 

┌─────────────┐
│    User     │
└──────┬──────┘
       │
       │ Demande une action
       ▼
┌─────────────────┐
│    AI Agent     │
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│ Comprendre      │
│ l'objectif      │
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│ Planifier les   │
│ sous-objectifs  │
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│ Choisir une     │
│ action / outil  │
└──────┬──────────┘
       │
       │ stop_reason = "tool_use"
       ▼
┌─────────────────┐
│ Exécuter        │
│ l'action        │
└──────┬──────────┘
       │
       │ tool_result
       ▼
┌─────────────────┐
│ Évaluer le      │
│ résultat        │
└──────┬──────────┘
       │
       ├─────────► Objectif atteint ?
       │
       │ Oui
       ▼
┌─────────────────┐
│ Action réalisée │
│ avec succès     │
└─────────────────┘

       ▲
       │ Non
       │
       └──────── Retour à la planification
                 pour une nouvelle action


* Est-ce que plusieurs Tool Calls suffisent à faire d'un système un Agent ?

Non, plusieurs Tool Calls peuvent effectuer plusieurs actions d'une façon paralléles et séquentielles mais ils manquent d'une boucle d'auto-décision qui présente un Agent et aussi la planification, l'adaptation aux résultats et la réévaluation.
