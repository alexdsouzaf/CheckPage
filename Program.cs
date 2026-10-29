using System.Reflection;
using CheckPage.Consultas;

var consultas = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(type => !type.IsInterface && !type.IsAbstract && typeof(IConsulta).IsAssignableFrom(type))
    .Select(type => (IConsulta)(Activator.CreateInstance(type)
        ?? throw new InvalidOperationException($"Não foi possível criar a consulta {type.Name}.")))
    .ToDictionary(consulta => consulta.Nome, StringComparer.OrdinalIgnoreCase);

if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
{
    Console.WriteLine("Uso: CheckPage <consulta>");
    Console.WriteLine("Consultas disponíveis:");

    foreach (var consulta in consultas.Keys.Order(StringComparer.OrdinalIgnoreCase))
        Console.WriteLine($"  {consulta}");

    return args.Length == 0 ? 2 : 0;
}

if (!consultas.TryGetValue(args[0], out var consultaSelecionada))
{
    Console.Error.WriteLine($"Consulta desconhecida: {args[0]}");
    Console.Error.WriteLine("Use 'CheckPage --help' para ver as consultas disponíveis.");
    return 2;
}

try
{
    return await consultaSelecionada.ExecutarAsync(args.Skip(1).ToArray());
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Erro não tratado na consulta '{consultaSelecionada.Nome}' ({ex.GetType().Name}).");
    return 1;
}
