global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentAssertions;
global using Moq;
global using StackExchange.Redis;
global using Xunit;
global using PerformanceEngine.Baseline.Domain.Domain;
global using PerformanceEngine.Baseline.Domain.Domain.Comparisons;
global using PerformanceEngine.Baseline.Domain.Domain.Tolerances;
global using PerformanceEngine.Baseline.Infrastructure.Persistence;
global using Baseline = PerformanceEngine.Baseline.Domain.Domain.Baselines.Baseline;
global using BaselineId = PerformanceEngine.Baseline.Domain.Domain.Baselines.BaselineId;

