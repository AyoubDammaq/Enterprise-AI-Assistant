
1. Qu'est-ce qu'un Tool Contract ?
Un Tool Contract est la définition claire de ce que le Tool accepte, de ce qu'il fait et de ce qu'il retourne.
2. Pourquoi le Tool doit-il valider ses paramètres ?
Parce que le fait qu'un LLM fournisse un paramètre ne signifie pas que ce paramètre est valide.
3. Que doit faire l'application si le Tool reçoit un paramètre invalide ?
Elle ne doit pas exécuter l'action.
L'application doit :
    - détecter l'erreur ;
    - empêcher l'exécution ;
    - retourner une erreur contrôlée ;
    - éventuellement permettre au LLM de générer une réponse compréhensible pour l'utilisateu
4. Pourquoi le LLM ne doit-il pas être considéré comme une source fiable ?
Parce que le LLM génère des résultats probabilistes, il ne garantit pas que chaque décision ou chaque paramètre produit est correct.