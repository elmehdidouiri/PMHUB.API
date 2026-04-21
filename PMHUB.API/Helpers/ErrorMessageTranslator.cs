using System.Text.RegularExpressions;

namespace PMHUB.API.Helpers
{
    public static class ErrorMessageTranslator
    {
        private static readonly Dictionary<string, string> ExactReplacements = new()
        {
            ["Une erreur interne du serveur s'est produite."] = "An internal server error occurred.",
            ["Une ou plusieurs erreurs de validation se sont produites."] = "One or more validation errors occurred.",
            ["Vous n'avez pas la permission d'effectuer cette action."] = "You do not have permission to perform this action.",
            ["Authentification requise pour accéder à cette ressource."] = "Authentication is required to access this resource.",
            ["Authentification requise pour accÃ©der Ã  cette ressource."] = "Authentication is required to access this resource.",
            ["Authentification requise pour accÃƒÂ©der ÃƒÂ  cette ressource."] = "Authentication is required to access this resource.",
            ["Session expirée ou invalide. Reconnectez-vous."] = "Your session has expired or is invalid. Please sign in again.",
            ["Session expirÃ©e ou invalide. Reconnectez-vous."] = "Your session has expired or is invalid. Please sign in again.",
            ["Session expirÃƒÂ©e ou invalide. Reconnectez-vous."] = "Your session has expired or is invalid. Please sign in again.",
            ["Accès refusé. Permissions insuffisantes."] = "Access denied. Insufficient permissions.",
            ["AccÃ¨s refusÃ©. Permissions insuffisantes."] = "Access denied. Insufficient permissions.",
            ["AccÃƒÂ¨s refusÃƒÂ©. Permissions insuffisantes."] = "Access denied. Insufficient permissions.",
            ["Le prénom est obligatoire."] = "First name is required.",
            ["Le prÃ©nom est obligatoire."] = "First name is required.",
            ["Le nom est obligatoire."] = "Name is required.",
            ["L'email est obligatoire."] = "Email is required.",
            ["Le mot de passe est obligatoire."] = "Password is required.",
            ["Le rôle est obligatoire."] = "Role is required.",
            ["Le rÃ´le est obligatoire."] = "Role is required.",
            ["Le département est obligatoire."] = "Department is required.",
            ["Le dÃ©partement est obligatoire."] = "Department is required.",
            ["Le budget est obligatoire."] = "Budget is required.",
            ["La date de début est obligatoire."] = "Start date is required.",
            ["La date de dÃ©but est obligatoire."] = "Start date is required.",
            ["Le type de projet est obligatoire."] = "Project type is required.",
            ["Le project type est obligatoire."] = "Project type is required.",
            ["Le nom de la ressource est obligatoire."] = "Resource name is required.",
            ["Le type de critère est obligatoire."] = "Criterion type is required.",
            ["Le type de critÃ¨re est obligatoire."] = "Criterion type is required.",
            ["Le score est obligatoire."] = "Score is required.",
            ["Le nom du sous-projet est obligatoire."] = "Subproject name is required.",
            ["Le fichier est obligatoire."] = "File is required.",
            ["Le type de document est obligatoire."] = "Document type is required.",
            ["Le BusinessUnit est obligatoire."] = "Business unit is required.",
            ["Le Plant est obligatoire."] = "Plant is required.",
            ["L'ID de l'entrée est requis"] = "Entry ID is required.",
            ["L'ID de l'entrÃ©e est requis"] = "Entry ID is required.",
            ["La décision est requise"] = "Decision is required.",
            ["La dÃ©cision est requise"] = "Decision is required.",
            ["Le projet est requis"] = "Project is required.",
            ["Le type d'allocation est requis"] = "Allocation type is required.",
            ["Le type de projet est requis"] = "Project type is required.",
            ["Le type de booking est requis"] = "Booking type is required.",
            ["La date est requise"] = "Date is required.",
            ["Format d'email invalide."] = "Invalid email format.",
            ["Le mot de passe doit contenir au moins 6 caractères."] = "Password must contain at least 6 characters.",
            ["Le mot de passe doit contenir au moins 6 caractÃ¨res."] = "Password must contain at least 6 characters.",
            ["Les heures doivent être entre 0 et 24"] = "Hours must be between 0 and 24.",
            ["Les heures doivent Ãªtre entre 0 et 24"] = "Hours must be between 0 and 24.",
            ["Le mois doit être entre 1 et 12"] = "Month must be between 1 and 12.",
            ["Le mois doit Ãªtre entre 1 et 12"] = "Month must be between 1 and 12.",
            ["Month must be between 1 and 12"] = "Month must be between 1 and 12.",
            ["Month must be between 1 and 12."] = "Month must be between 1 and 12.",
            ["Email et mot de passe sont obligatoires."] = "Email and password are required.",
            ["Aucune donnée trouvée pour les filtres demandés."] = "No data was found for the requested filters.",
            ["Aucune donnÃ©e trouvÃ©e pour les filtres demandÃ©s."] = "No data was found for the requested filters.",
            ["BusinessUnit introuvable."] = "Business unit was not found.",
            ["Département introuvable."] = "Department was not found.",
            ["DÃ©partement introuvable."] = "Department was not found.",
            ["Plant introuvable."] = "Plant was not found.",
            ["Projet introuvable."] = "Project was not found.",
            ["Rôle introuvable."] = "Role was not found.",
            ["RÃ´le introuvable."] = "Role was not found.",
            ["Domaine de solution introuvable."] = "Solution domain was not found.",
            ["Technologie introuvable."] = "Technology was not found.",
            ["Le booking des heures d'intern doit être effectué par un utilisateur normal."] = "Intern hour booking must be performed by a normal user.",
            ["Le booking des heures d'intern doit Ãªtre effectuÃ© par un utilisateur normal."] = "Intern hour booking must be performed by a normal user.",
            ["Vous n'êtes pas membre de ce projet."] = "You must be a member of this project to perform this action.",
            ["Vous n'Ãªtes pas membre de ce projet."] = "You must be a member of this project to perform this action.",
            ["Vous avez déjà un booking pour cette journée sur ce projet. Vous pouvez le modifier."] = "You already have a booking for this project on that day. Update the existing entry instead.",
            ["Vous avez dÃ©jÃ  un booking pour cette journÃ©e sur ce projet. Vous pouvez le modifier."] = "You already have a booking for this project on that day. Update the existing entry instead.",
            ["Vous ne pouvez modifier que vos propres allocations."] = "You can only update your own bookings.",
            ["Vous ne pouvez supprimer que vos propres allocations."] = "You can only delete your own bookings.",
            ["Vous ne pouvez pas changer le type de booking après une décision premium."] = "You cannot change the booking type after a premium decision has been made.",
            ["Vous ne pouvez pas changer le type de booking aprÃ¨s une dÃ©cision premium."] = "You cannot change the booking type after a premium decision has been made.",
            ["Cette entrée n'est pas premium."] = "This entry is not a premium booking.",
            ["Cette entrÃ©e n'est pas premium."] = "This entry is not a premium booking.",
            ["Cette entrée a déjà été traitée."] = "This entry has already been processed.",
            ["Cette entrÃ©e a dÃ©jÃ  Ã©tÃ© traitÃ©e."] = "This entry has already been processed.",
            ["Impossible de supprimer cet intern car il est déjà alloué à un ou plusieurs projets."] = "This intern cannot be deleted because they are already allocated to one or more projects.",
            ["Impossible de supprimer cet intern car il est dÃ©jÃ  allouÃ© Ã  un ou plusieurs projets."] = "This intern cannot be deleted because they are already allocated to one or more projects.",
            ["Le superviseur doit être un utilisateur normal."] = "The selected supervisor must be a normal user.",
            ["Le superviseur doit Ãªtre un utilisateur normal."] = "The selected supervisor must be a normal user.",
            ["Utilisateur non autorisé."] = "You are not authorized to perform this action.",
            ["Utilisateur non autorisÃ©."] = "You are not authorized to perform this action.",
            ["Seul l'admin ou le superviseur concerné peut gérer cet intern."] = "Only an administrator or the assigned supervisor can manage this intern.",
            ["Seul l'admin ou le superviseur concernÃ© peut gÃ©rer cet intern."] = "Only an administrator or the assigned supervisor can manage this intern.",
            ["Le versioning est autorisé uniquement pour les fichiers Compliance."] = "Versioning is only available for Compliance files.",
            ["L'email ne peut pas être vide."] = "Email cannot be empty.",
            ["Cet utilisateur est déjà approuvé."] = "This user has already been approved.",
            ["Ce compte est désactivé."] = "This account is deactivated.",
            ["Ce compte n'est pas encore approuvé."] = "This account has not been approved yet.",
            ["Email ou mot de passe incorrect."] = "Invalid email or password.",
            ["Le fichier est vide ou manquant."] = "The file is missing or empty.",
            ["User is not authenticated."] = "You must be signed in to perform this action.",
            ["Authenticated user could not be resolved."] = "The authenticated user could not be resolved from the current token."
        };

        private static readonly (Regex Pattern, string Replacement)[] RegexReplacements =
        {
            (new Regex("^'(?<name>.+)' avec l'identifiant '(?<key>.+)' est introuvable\\.$", RegexOptions.Compiled), "'${name}' with identifier '${key}' was not found."),
            (new Regex("^'(?<name>.+)' avec la valeur '(?<key>.+)' existe déjà\\.$", RegexOptions.Compiled), "'${name}' with value '${key}' already exists."),
            (new Regex("^'(?<name>.+)' avec la valeur '(?<key>.+)' existe dÃ©jÃ \\.$", RegexOptions.Compiled), "'${name}' with value '${key}' already exists."),
            (new Regex(" est obligatoire\\.", RegexOptions.Compiled), " is required."),
            (new Regex(" sont obligatoires\\.", RegexOptions.Compiled), " are required."),
            (new Regex(" est requis\\.?$", RegexOptions.Compiled), " is required."),
            (new Regex(" est requise\\.?$", RegexOptions.Compiled), " is required."),
            (new Regex(" sont requis\\.?$", RegexOptions.Compiled), " are required."),
            (new Regex(" sont requises\\.?$", RegexOptions.Compiled), " are required."),
            (new Regex(" ne peut pas être vide\\.", RegexOptions.Compiled), " cannot be empty."),
            (new Regex(" ne peut pas Ãªtre vide\\.", RegexOptions.Compiled), " cannot be empty."),
            (new Regex(" ne peut pas dépasser (?<n>\\d+) caractères\\.", RegexOptions.Compiled), " cannot exceed ${n} characters."),
            (new Regex(" ne peut pas dÃ©passer (?<n>\\d+) caractÃ¨res\\.", RegexOptions.Compiled), " cannot exceed ${n} characters."),
            (new Regex(" doit être positive\\.", RegexOptions.Compiled), " must be positive."),
            (new Regex(" doit Ãªtre positive\\.", RegexOptions.Compiled), " must be positive."),
            (new Regex(" doit être positif\\.", RegexOptions.Compiled), " must be positive."),
            (new Regex(" doit Ãªtre positif\\.", RegexOptions.Compiled), " must be positive."),
            (new Regex(" doivent être positives\\.", RegexOptions.Compiled), " must be positive."),
            (new Regex(" doivent Ãªtre positives\\.", RegexOptions.Compiled), " must be positive."),
            (new Regex(" doivent être entre (?<min>\\d+) et (?<max>\\d+)\\.?$", RegexOptions.Compiled), " must be between ${min} and ${max}."),
            (new Regex(" doit être entre (?<min>\\d+) et (?<max>\\d+)\\.?$", RegexOptions.Compiled), " must be between ${min} and ${max}."),
            (new Regex(" doit contenir au moins (?<n>\\d+) caractères\\.", RegexOptions.Compiled), " must contain at least ${n} characters."),
            (new Regex(" doit contenir au moins (?<n>\\d+) caractÃ¨res\\.", RegexOptions.Compiled), " must contain at least ${n} characters."),
            (new Regex(" doit être supérieur ou égal à la date de début\\.", RegexOptions.Compiled), " must be greater than or equal to the start date."),
            (new Regex(" doit Ãªtre supÃ©rieur ou Ã©gal Ã  la date de dÃ©but\\.", RegexOptions.Compiled), " must be greater than or equal to the start date."),
            (new Regex(" doit être supérieure à la date de début\\.", RegexOptions.Compiled), " must be greater than the start date."),
            (new Regex(" doit Ãªtre supÃ©rieure Ã  la date de dÃ©but\\.", RegexOptions.Compiled), " must be greater than the start date."),
            (new Regex(" n'appartient pas à ce projet\\.", RegexOptions.Compiled), " does not belong to this project."),
            (new Regex(" n'appartient pas Ã  ce projet\\.", RegexOptions.Compiled), " does not belong to this project."),
            (new Regex(" n'appartient pas à cette allocation intern\\.", RegexOptions.Compiled), " does not belong to this intern allocation."),
            (new Regex(" n'appartient pas Ã  cette allocation intern\\.", RegexOptions.Compiled), " does not belong to this intern allocation."),
            (new Regex(" est déjà membre du projet\\.", RegexOptions.Compiled), " is already a project member."),
            (new Regex(" est dÃ©jÃ  membre du projet\\.", RegexOptions.Compiled), " is already a project member."),
            (new Regex(" est déjà défini pour ce projet\\.", RegexOptions.Compiled), " is already defined for this project."),
            (new Regex(" est dÃ©jÃ  dÃ©fini pour ce projet\\.", RegexOptions.Compiled), " is already defined for this project."),
            (new Regex(" ne peut pas avoir un parent\\.", RegexOptions.Compiled), " cannot have a parent project."),
            (new Regex(" doit avoir un projet parent\\.", RegexOptions.Compiled), " must have a parent project."),
            (new Regex(" ne peut pas être son propre parent\\.", RegexOptions.Compiled), " cannot be its own parent."),
            (new Regex(" ne peut pas Ãªtre son propre parent\\.", RegexOptions.Compiled), " cannot be its own parent."),
            (new Regex("Seul le superviseur de l'intern peut booker ses heures\\.", RegexOptions.Compiled), "Only the intern's supervisor can book their hours."),
            (new Regex("Les heures bookées dépassent les heures allouées à cet intern sur le projet\\.", RegexOptions.Compiled), "Booked hours exceed the hours allocated to this intern on the project."),
            (new Regex("Les heures bookÃ©es dÃ©passent les heures allouÃ©es Ã  cet intern sur le projet\\.", RegexOptions.Compiled), "Booked hours exceed the hours allocated to this intern on the project."),
            (new Regex("Les heures allouées ne peuvent pas être inférieures aux heures déjà bookées\\.", RegexOptions.Compiled), "Allocated hours cannot be lower than already booked hours."),
            (new Regex("Les heures allouÃ©es ne peuvent pas Ãªtre infÃ©rieures aux heures dÃ©jÃ  bookÃ©es\\.", RegexOptions.Compiled), "Allocated hours cannot be lower than already booked hours."),
            (new Regex("Une task ne peut être assignée qu'à un membre du projet ou à une allocation stagiaire, pas aux deux\\.", RegexOptions.Compiled), "A task can only be assigned to a project member or an intern allocation, not both."),
            (new Regex("Une task ne peut Ãªtre assignÃ©e qu'Ã  un membre du projet ou Ã  une allocation stagiaire, pas aux deux\\.", RegexOptions.Compiled), "A task can only be assigned to a project member or an intern allocation, not both."),
            (new Regex("Le membre sélectionné n'appartient pas à ce projet\\.", RegexOptions.Compiled), "The selected team member does not belong to this project."),
            (new Regex("Le membre sÃ©lectionnÃ© n'appartient pas Ã  ce projet\\.", RegexOptions.Compiled), "The selected team member does not belong to this project."),
            (new Regex("L'allocation stagiaire sélectionnée n'appartient pas à ce projet\\.", RegexOptions.Compiled), "The selected intern allocation does not belong to this project."),
            (new Regex("L'allocation stagiaire sÃ©lectionnÃ©e n'appartient pas Ã  ce projet\\.", RegexOptions.Compiled), "The selected intern allocation does not belong to this project."),
            (new Regex("Une task de développement doit contenir au moins un effort Dev, UX ou Testing\\.", RegexOptions.Compiled), "A development task must contain at least one Dev, UX, or Testing effort."),
            (new Regex("Une task de dÃ©veloppement doit contenir au moins un effort Dev, UX ou Testing\\.", RegexOptions.Compiled), "A development task must contain at least one Dev, UX, or Testing effort."),
            (new Regex("Le champ 'Hours' ne doit pas être utilisé pour une task de développement\\.", RegexOptions.Compiled), "The 'Hours' field must not be used for a development task."),
            (new Regex("Le champ 'Hours' ne doit pas Ãªtre utilisÃ© pour une task de dÃ©veloppement\\.", RegexOptions.Compiled), "The 'Hours' field must not be used for a development task."),
            (new Regex("Le champ 'Hours' est obligatoire pour cette catégorie de task\\.", RegexOptions.Compiled), "The 'Hours' field is required for this task category."),
            (new Regex("Le champ 'Hours' est obligatoire pour cette catÃ©gorie de task\\.", RegexOptions.Compiled), "The 'Hours' field is required for this task category."),
            (new Regex("Les heures Dev, UX et Testing sont réservées aux tasks de développement\\.", RegexOptions.Compiled), "Dev, UX, and Testing hours are only allowed for development tasks."),
            (new Regex("Les heures Dev, UX et Testing sont rÃ©servÃ©es aux tasks de dÃ©veloppement\\.", RegexOptions.Compiled), "Dev, UX, and Testing hours are only allowed for development tasks."),
            (new Regex("Les story points sont réservés aux tasks de développement\\.", RegexOptions.Compiled), "Story points are only allowed for development tasks."),
            (new Regex("Les story points sont rÃ©servÃ©s aux tasks de dÃ©veloppement\\.", RegexOptions.Compiled), "Story points are only allowed for development tasks."),
            (new Regex("Le statut '(?<status>.+)' n'est pas cohérent avec la phase '(?<phase>.+)'\\.", RegexOptions.Compiled), "Status '${status}' is not valid for phase '${phase}'."),
            (new Regex("Le statut '(?<status>.+)' n'est pas cohÃ©rent avec la phase '(?<phase>.+)'\\.", RegexOptions.Compiled), "Status '${status}' is not valid for phase '${phase}'."),
            (new Regex("Un roadblock résolu doit avoir une date de résolution\\.", RegexOptions.Compiled), "A resolved roadblock must have a resolution date."),
            (new Regex("Un roadblock rÃ©solu doit avoir une date de rÃ©solution\\.", RegexOptions.Compiled), "A resolved roadblock must have a resolution date."),
            (new Regex("Un roadblock ouvert ne doit pas avoir de date de résolution\\.", RegexOptions.Compiled), "An open roadblock must not have a resolution date."),
            (new Regex("Un roadblock ouvert ne doit pas avoir de date de rÃ©solution\\.", RegexOptions.Compiled), "An open roadblock must not have a resolution date."),
            (new Regex("Le fichier dépasse la taille maximale autorisée de (?<n>\\d+) Mo\\.", RegexOptions.Compiled), "The file exceeds the maximum allowed size of ${n} MB."),
            (new Regex("Le fichier dÃ©passe la taille maximale autorisÃ©e de (?<n>\\d+) Mo\\.", RegexOptions.Compiled), "The file exceeds the maximum allowed size of ${n} MB."),
            (new Regex("Le type de fichier '(?<type>.+)' n'est pas autorisé\\.", RegexOptions.Compiled), "The file type '${type}' is not allowed."),
            (new Regex("Le type de fichier '(?<type>.+)' n'est pas autorisÃ©\\.", RegexOptions.Compiled), "The file type '${type}' is not allowed."),
            (new Regex("Le nom ne peut pas dépasser (?<n>\\d+) caractères\\.", RegexOptions.Compiled), "Name cannot exceed ${n} characters."),
            (new Regex("Le nom ne peut pas dÃ©passer (?<n>\\d+) caractÃ¨res\\.", RegexOptions.Compiled), "Name cannot exceed ${n} characters."),
            (new Regex("La description ne peut pas dépasser (?<n>\\d+) caractères\\.", RegexOptions.Compiled), "Description cannot exceed ${n} characters."),
            (new Regex("La description ne peut pas dÃ©passer (?<n>\\d+) caractÃ¨res\\.", RegexOptions.Compiled), "Description cannot exceed ${n} characters."),
            (new Regex("Les notes ne peuvent pas dépasser (?<n>\\d+) caractères\\.", RegexOptions.Compiled), "Notes cannot exceed ${n} characters."),
            (new Regex("Les notes ne peuvent pas dÃ©passer (?<n>\\d+) caractÃ¨res\\.", RegexOptions.Compiled), "Notes cannot exceed ${n} characters."),
            (new Regex("La contribution digitale doit être positive\\.", RegexOptions.Compiled), "Digital contribution must be positive."),
            (new Regex("La contribution digitale doit Ãªtre positive\\.", RegexOptions.Compiled), "Digital contribution must be positive."),
            (new Regex("Le coût économisé doit être positif\\.", RegexOptions.Compiled), "Cost saving must be positive."),
            (new Regex("Le coÃ»t Ã©conomisÃ© doit Ãªtre positif\\.", RegexOptions.Compiled), "Cost saving must be positive."),
            (new Regex("Le pourcentage doit être entre (?<min>\\d+) et (?<max>\\d+)\\.", RegexOptions.Compiled), "Percentage must be between ${min} and ${max}."),
            (new Regex("Le pourcentage doit Ãªtre entre (?<min>\\d+) et (?<max>\\d+)\\.", RegexOptions.Compiled), "Percentage must be between ${min} and ${max}."),
            (new Regex("Les heures estimées doivent être positives\\.", RegexOptions.Compiled), "Estimated hours must be positive."),
            (new Regex("Les heures estimÃ©es doivent Ãªtre positives\\.", RegexOptions.Compiled), "Estimated hours must be positive."),
            (new Regex("Les heures réelles doivent être positives\\.", RegexOptions.Compiled), "Actual hours must be positive."),
            (new Regex("Les heures rÃ©elles doivent Ãªtre positives\\.", RegexOptions.Compiled), "Actual hours must be positive."),
            (new Regex("La date de fin doit être supérieure à la date de début\\.", RegexOptions.Compiled), "End date must be greater than the start date."),
            (new Regex("La date de fin doit Ãªtre supÃ©rieure Ã  la date de dÃ©but\\.", RegexOptions.Compiled), "End date must be greater than the start date."),
            (new Regex("La date estimée doit être supérieure à la date de début\\.", RegexOptions.Compiled), "Estimated due date must be greater than the start date."),
            (new Regex("La date estimÃ©e doit Ãªtre supÃ©rieure Ã  la date de dÃ©but\\.", RegexOptions.Compiled), "Estimated due date must be greater than the start date."),
            (new Regex("Le fichier est vide ou manquant\\.", RegexOptions.Compiled), "The file is missing or empty."),
            (new Regex("L'email ne peut pas être vide\\.", RegexOptions.Compiled), "Email cannot be empty."),
            (new Regex("Vous ne pouvez modifier que vos propres allocations\\.", RegexOptions.Compiled), "You can only update your own bookings."),
            (new Regex("Vous ne pouvez supprimer que vos propres allocations\\.", RegexOptions.Compiled), "You can only delete your own bookings."),
            (new Regex("Vous ne pouvez pas changer le type de booking après une décision premium\\.", RegexOptions.Compiled), "You cannot change the booking type after a premium decision has been made."),
            (new Regex("Vous ne pouvez pas changer le type de booking aprÃ¨s une dÃ©cision premium\\.", RegexOptions.Compiled), "You cannot change the booking type after a premium decision has been made."),
            (new Regex("Vous n'êtes pas membre de ce projet\\.", RegexOptions.Compiled), "You must be a member of this project to perform this action."),
            (new Regex("Vous n'Ãªtes pas membre de ce projet\\.", RegexOptions.Compiled), "You must be a member of this project to perform this action."),
            (new Regex("Impossible de supprimer cet intern car il est déjà alloué à un ou plusieurs projets\\.", RegexOptions.Compiled), "This intern cannot be deleted because they are already allocated to one or more projects."),
            (new Regex("Impossible de supprimer cet intern car il est dÃ©jÃ  allouÃ© Ã  un ou plusieurs projets\\.", RegexOptions.Compiled), "This intern cannot be deleted because they are already allocated to one or more projects."),
            (new Regex("Le superviseur doit être un utilisateur normal\\.", RegexOptions.Compiled), "The selected supervisor must be a normal user."),
            (new Regex("Le superviseur doit Ãªtre un utilisateur normal\\.", RegexOptions.Compiled), "The selected supervisor must be a normal user."),
            (new Regex("Utilisateur non autorisé\\.", RegexOptions.Compiled), "You are not authorized to perform this action."),
            (new Regex("Utilisateur non autorisÃ©\\.", RegexOptions.Compiled), "You are not authorized to perform this action."),
            (new Regex("Seul l'admin ou le superviseur concerné peut gérer cet intern\\.", RegexOptions.Compiled), "Only an administrator or the assigned supervisor can manage this intern."),
            (new Regex("Seul l'admin ou le superviseur concernÃ© peut gÃ©rer cet intern\\.", RegexOptions.Compiled), "Only an administrator or the assigned supervisor can manage this intern."),
            (new Regex("Le versioning est autorisé uniquement pour les fichiers Compliance\\.", RegexOptions.Compiled), "Versioning is only available for Compliance files."),
            (new Regex("Cette entrée n'est pas premium\\.", RegexOptions.Compiled), "This entry is not a premium booking."),
            (new Regex("Cette entrÃ©e n'est pas premium\\.", RegexOptions.Compiled), "This entry is not a premium booking."),
            (new Regex("Cette entrée a déjà été traitée\\.", RegexOptions.Compiled), "This entry has already been processed."),
            (new Regex("Cette entrÃ©e a dÃ©jÃ  Ã©tÃ© traitÃ©e\\.", RegexOptions.Compiled), "This entry has already been processed."),
            (new Regex("Ce compte est désactivé\\.", RegexOptions.Compiled), "This account is deactivated."),
            (new Regex("Ce compte n'est pas encore approuvé\\.", RegexOptions.Compiled), "This account has not been approved yet."),
            (new Regex("Cet utilisateur est déjà approuvé\\.", RegexOptions.Compiled), "This user has already been approved."),
            (new Regex("Email ou mot de passe incorrect\\.", RegexOptions.Compiled), "Invalid email or password."),
            (new Regex("Authenticated user could not be resolved\\.", RegexOptions.Compiled), "The authenticated user could not be resolved from the current token."),
            (new Regex("User is not authenticated\\.", RegexOptions.Compiled), "You must be signed in to perform this action.")
        };

        public static string Translate(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            if (ExactReplacements.TryGetValue(message, out var exact))
            {
                return exact;
            }

            var translated = message;
            foreach (var (pattern, replacement) in RegexReplacements)
            {
                translated = pattern.Replace(translated, replacement);
            }

            return translated;
        }

        public static IDictionary<string, string[]>? Translate(IDictionary<string, string[]>? errors)
        {
            if (errors is null)
            {
                return null;
            }

            return errors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(Translate).ToArray());
        }

        public static string BuildValidationSummary(IDictionary<string, string[]>? errors, string fallbackMessage = "Validation failed.")
        {
            if (errors is null || errors.Count == 0)
            {
                return fallbackMessage;
            }

            var translatedErrors = Translate(errors);
            var errorCount = translatedErrors?.Sum(kvp => kvp.Value.Length) ?? 0;
            var firstError = translatedErrors?
                .SelectMany(kvp => kvp.Value)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

            if (errorCount <= 1 && !string.IsNullOrWhiteSpace(firstError))
            {
                return firstError;
            }

            var fieldCount = translatedErrors?.Count ?? 0;
            return $"Validation failed for {fieldCount} field{(fieldCount == 1 ? string.Empty : "s")}.";
        }
    }
}
