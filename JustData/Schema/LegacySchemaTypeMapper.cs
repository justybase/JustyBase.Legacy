using AppBase.Common.Enums;
using JustData.Application.Schema;

namespace JustyBaseLegacy.UI.Schema;

public static class LegacySchemaTypeMapper
{
    public static SchemaNodeKind Map(TypeInDatabase type) => type switch
    {
        TypeInDatabase.table or TypeInDatabase.thisExternal or TypeInDatabase.baseTables => SchemaNodeKind.Table,
        TypeInDatabase.view or TypeInDatabase.baseViews => SchemaNodeKind.View,
        TypeInDatabase.procedure or TypeInDatabase.baseProcedures => SchemaNodeKind.Procedure,
        TypeInDatabase.function or TypeInDatabase.baseFunctions => SchemaNodeKind.Function,
        TypeInDatabase.db2alias => SchemaNodeKind.Alias,
        TypeInDatabase.db2nickname => SchemaNodeKind.Nickname,
        TypeInDatabase.synonym or TypeInDatabase.baseSynonyms => SchemaNodeKind.Synonym,
        TypeInDatabase.sequence or TypeInDatabase.baseSequence => SchemaNodeKind.Sequence,
        _ => SchemaNodeKind.Unknown
    };
}
