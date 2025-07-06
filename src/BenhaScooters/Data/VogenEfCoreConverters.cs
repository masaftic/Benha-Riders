using BenhaScooters.Domain;
using Vogen;

namespace BenhaScooters.Data;

[EfCoreConverter<TodoId>]
[EfCoreConverter<TodoTitle>]
[EfCoreConverter<TodoPriority>]
public partial class VogenEfCoreConverters;
