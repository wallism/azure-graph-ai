using CloudGraphAI.AI.Plugins;
using NUnit.Framework;

namespace CloudGraphAI.Tests;

[TestFixture]
public class CypherSafetyTests
{
    [Test]
    public void PrepareReadOnlyQuery_WithSqlGroupBy_ThrowsCypherGuidance()
    {
        const string query = """
                             MATCH (m:MonthResourceCost)
                             WHERE m.resourceGroupName IS NOT NULL
                             RETURN m.resourceGroupName, sum(m.cost) AS totalCost, m.currency
                             GROUP BY m.resourceGroupName, m.currency
                             ORDER BY totalCost DESC
                             LIMIT 10
                             """;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CypherSafety.PrepareReadOnlyQuery(query, 100));

        Assert.That(ex!.Message, Does.Contain("Cypher does not support SQL-style GROUP BY"));
    }

    [Test]
    public void PrepareReadOnlyQuery_WithCypherAggregation_AllowsImplicitGrouping()
    {
        const string query = """
                             MATCH (m:MonthResourceCost)
                             WHERE m.resourceGroupName IS NOT NULL
                             RETURN m.resourceGroupName, sum(m.cost) AS totalCost, m.currency
                             ORDER BY totalCost DESC
                             LIMIT 10
                             """;

        var result = CypherSafety.PrepareReadOnlyQuery(query, 100);

        Assert.That(result, Is.EqualTo(query));
    }
}
