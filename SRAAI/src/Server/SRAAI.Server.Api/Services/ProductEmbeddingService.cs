using SRAAI.Server.Api.Models.Products;

namespace SRAAI.Server.Api.Services;

/// <summary>
/// Approaches to implement text search:
/// 1- Simple string matching (e.g., `Contains` method).
/// 2- Full-text search using database capabilities (e.g., PostgreSQL's full-text search).
/// 3- Vector-based search using embeddings (e.g., using OpenAI's embeddings).
/// This service implements vector-based search using embeddings that has the following advantages:
///     - More accurate search results based on semantic meaning rather than just similarity matching.
///     - Multi-language support, as embeddings can capture the meaning of words across different languages.
/// And has the following disadvantages:
///     - Requires additional processing to generate embeddings for the text.
///     - Require more storage space for embeddings compared to simple text search.
/// The simple full-text search would be enough for product search case, but we have implemented the vector-based search to demonstrate how to use embeddings in the project.
/// </summary>
public partial class ProductEmbeddingService
{
    private const float SIMILARITY_THRESHOLD = 0.85f;

    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private IWebHostEnvironment env = default!;
    [AutoInject] private IServiceProvider serviceProvider = default!;

    public async Task<IQueryable<Product>> GetProductsBySearchQuery(string searchQuery, CancellationToken cancellationToken)
    {
        var embeddedUserQuery = await EmbedText(searchQuery, cancellationToken);
        if (embeddedUserQuery is null)
            return dbContext.Products.Where(p => p.Name!.Contains(searchQuery) || p.Category!.Name!.Contains(searchQuery));
        var value = embeddedUserQuery.Value.ToArray();
        return dbContext.Products
            .Where(p => p.Embedding != null && EF.Functions.VectorDistance("cosine", p.Embedding, value!) < SIMILARITY_THRESHOLD).OrderBy(p => EF.Functions.VectorDistance("cosine", p.Embedding!, value!));
    }

    public async Task Embed(Product product, CancellationToken cancellationToken)
    {
        await dbContext.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);

        // TODO: Needs to be improved.
        var embedding = await EmbedText($@"
Name: **{product.Name}**
Manufacture: **{product.Category!.Name}**
Description: {product.DescriptionText}
Appearance: {product.PrimaryImageAltText}", cancellationToken);

        if (embedding.HasValue)
        {
            product.Embedding = embedding.Value.ToArray();
        }
    }

    private async Task<ReadOnlyMemory<float>?> EmbedText(string input, CancellationToken cancellationToken)
    {
        if (AppDbContext.IsEmbeddingEnabled is false)
            return null;
        var embeddingGenerator = serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        if (embeddingGenerator is null)
            return env.IsDevelopment() ? null : throw new InvalidOperationException("Embedding generator is not registered.");

        input = $@"
Name: **{input}**
Manufacture: **{input}**
Description: {input}
Appearance: {input}";


        var embedding = await embeddingGenerator.GenerateVectorAsync(input, options: new() { }, cancellationToken);
        return embedding.ToArray();
    }
}
