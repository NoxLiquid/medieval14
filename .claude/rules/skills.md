Before starting any task, you are REQUIRED to:

1. Check the `skills.md` file located in `.claude/skills/` for the current project.

   * If the file exists — carefully study it before taking any action.
   * If the file does not exist — assume it is empty and will be created during the course of work.

2. Use `.claude/skills/skills.md` as the primary global source of current project knowledge:

   * existing mechanics
   * implementation rules
   * architectural decisions
   * previously identified limitations and bugs

3. For each separate logical segment of the project, you MUST create and maintain a dedicated `.md` file inside `.claude/skills/`:

   * each file must represent one coherent system/feature/domain
   * examples: `combat.md`, `economy.md`, `inventory.md`, `ai_behavior.md`
   * do not mix unrelated systems into a single file

4. Before starting the implementation of any new feature or change:

   * determine what skills/knowledge are required for the task
   * check whether they already exist in the relevant `.claude/skills/*.md` file
   * if not — explicitly mark that the skill is missing

5. During work, you are REQUIRED to update the relevant `.md` file(s):

   * add newly discovered knowledge
   * document new implementation patterns
   * describe errors and their solutions
   * refine existing entries when new information appears

6. After each significant step:

   * update the relevant `.claude/skills/*.md` file immediately (do not postpone until the end of the task)
   * ensure all files remain up-to-date and useful for future development

7. If any uncertainty arises during implementation:

   * first check the relevant `.claude/skills/*.md` file
   * if the information is not found — explicitly record the gap in the appropriate file and continue working without ignoring the issue

Main goal: the `.claude/skills/` directory must evolve into a structured knowledge base, where `skills.md` serves as a global overview and each subsystem has its own continuously updated documentation file.
