namespace Atrufulgium.FrontTick.Compiler.Datapack {
    /// <summary>
    /// The type of files a datapack can have, as defined by what folder it
    /// resides in: in <tt>data/namespace/folder/file</tt>, this is the
    /// <tt>folder</tt> part.
    /// </summary>
    public class DatapackLocation {

        readonly private string folderName;
        private DatapackLocation(string folderName) {
            this.folderName = folderName;
        }

        public static implicit operator string(DatapackLocation t) => t.folderName;

        private static readonly char slash = System.IO.Path.DirectorySeparatorChar;

        // lol
        // as if i'm ever going to use all of these
        public static readonly DatapackLocation Advancements = new("advancements");
        public static readonly DatapackLocation BlockTags = new($"tags{slash}blocks");
        public static readonly DatapackLocation DamageTypes = new("damage_type");
        public static readonly DatapackLocation DamageTypeTags = new($"tags{slash}damage_type");
        public static readonly DatapackLocation EntityTypeTags = new($"tags{slash}entity_types");
        public static readonly DatapackLocation Functions = new("functions");
        public static readonly DatapackLocation FunctionTags = new($"tags{slash}functions");
        public static readonly DatapackLocation GameEventTags = new($"tags{slash}game_events");
        public static readonly DatapackLocation ItemModifiers = new("item_modifiers");
        public static readonly DatapackLocation ItemTags = new($"tags{slash}items");
        public static readonly DatapackLocation LootTables = new("loot_tables");
        public static readonly DatapackLocation Predicates = new("predicates");
        public static readonly DatapackLocation Recipes = new("recipes");
        public static readonly DatapackLocation Structures = new("structures");
    }
}
