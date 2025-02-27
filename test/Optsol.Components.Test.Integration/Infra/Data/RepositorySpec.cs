using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Optsol.Components.Infra.Data.Pagination;
using Optsol.Components.Infra.UoW;
using Optsol.Components.Test.Utils.Data.Contexts;
using Optsol.Components.Test.Utils.Data.Entities;
using Optsol.Components.Test.Utils.Data.Entities.ValueObjecs;
using Optsol.Components.Test.Utils.Entity.Entities;
using Optsol.Components.Test.Utils.Repositories.Core;
using Optsol.Components.Test.Utils.Repositories.Deletable;
using Optsol.Components.Test.Utils.Seed;
using Optsol.Components.Test.Utils.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static Optsol.Components.Test.Utils.Seed.Seed;

namespace Optsol.Components.Test.Integration.Infra.Data
{
    public class RepositorySpec
    {
        private static ServiceProvider GetProviderConfiguredServicesFromContext()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddContext<Context>(options =>
            {
                options
                    .EnabledInMemory()
                    .EnabledLogging();

                options
                    .ConfigureRepositories<ITestReadRepository, TestReadRepository>("Optsol.Components.Test.Utils");
            });

            return services.BuildServiceProvider();
        }

        private static ServiceProvider GetProviderConfiguredServicesFromDeletableContext()
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddContext<DeletableContext>(options =>
            {
                options
                    .EnabledInMemory()
                    .EnabledLogging();

                options
                    .ConfigureRepositories<ITestDeletableReadRepository, TestDeletableReadRepository>("Optsol.Components.Test.Utils");
            });

            return services.BuildServiceProvider();
        }

       

        public class HttpContextAccessorTest : IHttpContextAccessor
        {
            public HttpContext HttpContext { get; set; }

            public HttpContextAccessorTest(string tenatHost)
            {
                HttpContext = new DefaultHttpContext();
                HttpContext.Request.Host = new HostString(tenatHost);
            }
        }

        [Trait("Infraestrutura", "Respositório de Leitura")]
        [Fact(DisplayName = "Deve obter todos registros pelo repositório")]
        public async Task Deve_Obter_Todos_Pelo_Repositorio()
        {
            //Given
            var numberItems = 3;
            var provider = GetProviderConfiguredServicesFromContext()
                .CreateTestEntitySeedInContext(numberItems);

            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();

            //When
            var entityResult = await testReadRepository.GetAllAsync();

            //Then
            entityResult.Should().HaveCount(numberItems);
        }

        [Trait("Infraestrutura", "Respositório de Leitura")]
        [Fact(DisplayName = "Deve obter o registro pelo id")]
        public async Task Deve_Obter_Por_Id_Pelo_Repositorio()
        {
            //Given
            var numberItems = 3;
            var entity = default(TestEntity);

            var provider = GetProviderConfiguredServicesFromContext()
                .CreateTestEntitySeedInContext(numberItems, (entityList) =>
                {
                    entity = entityList.First();
                });

            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();

            //When
            var entityResult = await testReadRepository.GetByIdAsync(entity.Id);

            //Then
            entityResult.Should().NotBeNull();
            entityResult.Nome.ToString().Should().Be(entity.Nome.ToString());
            entityResult.Email.ToString().Should().Be(entity.Email.ToString());
        }

        [Trait("Infraestrutura", "Respositório de Leitura")]
        [Fact(DisplayName = "Deve obter os registros por uma lista de ids")]
        public async Task Deve_Obter_Por_Lista_Id_Pelo_Repositorio()
        {
            //Given
            var numberItems = 4;
            Guid[] ids = default;

            var provider = GetProviderConfiguredServicesFromContext()
                .CreateTestEntitySeedInContext(numberItems, (entityList) =>
                {
                    ids = entityList.Select(s => s.Id).ToArray();
                });

            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();

            //When
            var entityResult = await testReadRepository.GetByIdsAsync(ids);

            //Then
            entityResult.Should().NotBeNull();
            entityResult.Should().HaveCount(numberItems);
        }

        public class ObterRegistroPaginadoParams : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var searchDto = new TestSearchDto();

                yield return new object[]
                {
                    new SearchRequest<TestSearchDto>
                    {
                        Search = searchDto,
                        Page = 0,
                        PageSize = 10
                    }
                };
                yield return new object[]
                {
                    new SearchRequest<TestSearchDto>
                    {
                        Search = searchDto,
                        Page = 2,
                        PageSize = 10
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Trait("Infraestrutura", "Respositório de Leitura")]
        [Theory(DisplayName = "Deve obter todos os registros com os critérios de paginação")]
        [ClassData(typeof(ObterRegistroPaginadoParams))]
        public async Task Deve_Obter_Registros_Paginados(SearchRequest<TestSearchDto> searchRequest)
        {
            //Given
            var numberItems = 100;
            var testEntityList = new List<TestEntity>();

            var provider = GetProviderConfiguredServicesFromContext()
                .CreateTestEntitySeedInContext(numberItems, (entityList) =>
                {
                    entityList = entityList.OrderBy(o => o.Nome.Nome);
                    testEntityList = entityList.ToList();
                });

            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();

            //When
            var searchResultList = await testReadRepository.GetAllAsync(searchRequest);

            //Then
            searchResultList.Should().NotBeNull();
            searchResultList.Page.Should().Be(searchRequest.Page);
            searchResultList.PageSize.Should().Be(searchRequest.PageSize);
            searchResultList.Items.Should().HaveCount(searchRequest.PageSize.Value.ToInt());
            searchResultList.TotalItems.Should().Be(searchRequest.PageSize.Value);

            var skip = searchResultList.Page <= 0 ? 1 : --searchResultList.Page * (searchResultList.PageSize ?? 0);

            searchResultList.Items.First().Id.Should().Be(testEntityList.Skip(skip.ToInt()).First().Id);
            searchResultList.Items.Last().Id.Should().Be(testEntityList.Skip(skip.ToInt()).Take(searchResultList.PageSize.Value.ToInt()).Last().Id);
            searchResultList.Total.Should().Be(testEntityList.Count);
        }

        public class ObterRegistroPaginadoSomenteFiltroParams : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var searchDto = new TestSearchOnlyDto();

                yield return new object[]
                {
                    new SearchRequest<TestSearchOnlyDto>
                    {
                        Search = searchDto,
                        Page = 0,
                        PageSize = 10
                    }
                };
                yield return new object[]
                {
                    new SearchRequest<TestSearchOnlyDto>
                    {
                        Search = searchDto,
                        Page = 2,
                        PageSize = 10
                    }
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Trait("Infraestrutura", "Respositório de Leitura")]
        [Theory(DisplayName = "Deve obter todos os registros somente com os filtros")]
        [ClassData(typeof(ObterRegistroPaginadoSomenteFiltroParams))]
        public async Task Deve_Obter_Registros_Paginados_Usando_Somente_Filtro(SearchRequest<TestSearchOnlyDto> searchRequest)
        {
            //Given
            var numberItems = 100;
            var testEntityList = new List<TestEntity>();

            var provider = GetProviderConfiguredServicesFromContext()
                .CreateTestEntitySeedInContext(numberItems, (entityList) =>
                {
                    testEntityList = entityList.ToList();
                });

            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();

            //When
            var searchResultList = await testReadRepository.GetAllAsync(searchRequest);

            //Then
            searchResultList.Should().NotBeNull();
            searchResultList.Page.Should().Be(searchRequest.Page);
            searchResultList.PageSize.Should().Be(searchRequest.PageSize);
            searchResultList.Items.Should().HaveCount(searchRequest.PageSize.Value.ToInt());
            searchResultList.TotalItems.Should().Be(searchRequest.PageSize.Value);

            var skip = searchResultList.Page <= 0 ? 1 : --searchResultList.Page * (searchResultList.PageSize ?? 0);

            searchResultList.Items.First().Id.Should().Be(testEntityList.Skip(skip.ToInt()).First().Id);
            searchResultList.Items.Last().Id.Should().Be(testEntityList.Skip(skip.ToInt()).Take(searchResultList.PageSize.Value.ToInt()).Last().Id);
            searchResultList.Total.Should().Be(testEntityList.Count);
        }

        [Trait("Infraestrutura", "Respositório de Leitura")]
        [Fact(DisplayName = "Não deve obter nenhum registro excluído logicamente")]
        public async Task Nao_Deve_Obter_Registros_Excluidos_Logicamente()
        {
            //Given
            var numberItems = 3;
            var numberDeletable = 2;

            var provider = GetProviderConfiguredServicesFromDeletableContext()
                .CreateDeletableTestEntitySeedInContext(numberItems, (entityList) =>
                {
                    foreach (var delete in entityList.Take(numberDeletable))
                    {
                        delete.Delete();
                    }
                });

            var testDeletableReadRepository = provider.GetRequiredService<ITestDeletableReadRepository>();

            //When
            var entitiesResult = await testDeletableReadRepository.GetAllAsync();

            //Then
            var totalNotDeletable = numberItems - numberDeletable;
            entitiesResult.Should().HaveCount(totalNotDeletable);
        }

        public class InserirNovosRegistrosParams : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                foreach (var entity in TestEntityList().Take(3))
                {
                    yield return new object[] { entity };
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Trait("Infraestrutura", "Respositório de Escrita")]
        [Theory(DisplayName = "Deve inserir o registro na base de dados")]
        [ClassData(typeof(InserirNovosRegistrosParams))]
        public async Task Deve_Inserir_Registro_Pelo_Repositorio(TestEntity entity)
        {
            //Given
            var provider = GetProviderConfiguredServicesFromContext();
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();
            var testWriteRepository = provider.GetRequiredService<ITestWriteRepository>();

            //When
            await testWriteRepository.InsertAsync(entity);
            await unitOfWork.CommitAsync();

            //Then
            var entityResult = await testReadRepository.GetByIdAsync(entity.Id);
            entityResult.Valid.Should().BeTrue();
            entityResult.Invalid.Should().BeFalse();
            entityResult.Notifications.Should().HaveCount(0);
            entityResult.Should().NotBeNull();
            entityResult.Nome.ToString().Should().Be(entity.Nome.ToString());
            entityResult.Email.ToString().Should().Be(entity.Email.ToString());
            entityResult.Ativo.Should().BeFalse();
        }

        [Trait("Infraestrutura", "Respositório de Escrita")]
        [Theory(DisplayName = "Deve atualizar o registro obtido da base de dados")]
        [ClassData(typeof(InserirNovosRegistrosParams))]
        public async Task Deve_Atualizar_Registro_Pelo_Repositorio(TestEntity entity)
        {
            //Given
            var provider = GetProviderConfiguredServicesFromContext();
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();
            var testWriteRepository = provider.GetRequiredService<ITestWriteRepository>();

            await testWriteRepository.InsertAsync(entity);
            await unitOfWork.CommitAsync();

            //When
            var updateResult = await testReadRepository.GetByIdAsync(entity.Id);
            var updateEntity = new TestEntity(updateResult.Id, new NomeValueObject(updateResult.Nome.Nome, "Atualizado"), updateResult.Email);

            await testWriteRepository.UpdateAsync(updateEntity);
            await unitOfWork.CommitAsync();

            //Then
            var entityResult = await testReadRepository.GetByIdAsync(entity.Id);
            entityResult.Should().NotBeNull();

            entityResult.Valid.Should().BeTrue();
            entityResult.Invalid.Should().BeFalse();
            entityResult.Notifications.Should().BeEmpty();

            entityResult.Nome.ToString().Should().Be(updateEntity.Nome.ToString());
            entityResult.Email.ToString().Should().Be(updateEntity.Email.ToString());
            entityResult.Ativo.Should().BeFalse();
        }

        [Trait("Infraestrutura", "Respositório de Escrita")]
        [Fact(DisplayName = "Deve remover o registro obtido da base de dados")]
        public async Task Deve_Remover_Registro_Pelo_Id_Pelo_Repositorio()
        {
            //Given
            var numberItems = 1;

            var entity = default(TestEntity);

            var provider = GetProviderConfiguredServicesFromContext()
                .CreateTestEntitySeedInContext(numberItems, (entityList) =>
                {
                    entity = entityList.First();
                });

            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();
            var testWriteRepository = provider.GetRequiredService<ITestWriteRepository>();

            //When
            await testWriteRepository.DeleteAsync(entity.Id);
            await unitOfWork.CommitAsync();

            //Then
            var entityResult = await testReadRepository.GetByIdAsync(entity.Id);
            entityResult.Should().BeNull();
        }

        [Trait("Infraestrutura", "Respositório de Escrita")]
        [Fact(DisplayName = "Não deve remover o registro se o Id for inválido")]
        public async Task Nao_Deve_Remover_Se_Id_For_Invalido()
        {
            //Given
            var numberItems = 3;
            var entityDelete = Guid.NewGuid();

            var provider = GetProviderConfiguredServicesFromContext()
                .CreateTestEntitySeedInContext(numberItems);

            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            var testReadRepository = provider.GetRequiredService<ITestReadRepository>();
            var testWriteRepository = provider.GetRequiredService<ITestWriteRepository>();

            //When
            await testWriteRepository.DeleteAsync(entityDelete);
            await unitOfWork.CommitAsync();

            //Then
            var entityResultList = await testReadRepository.GetAllAsync();
            entityResultList.Should().NotBeNull();
            entityResultList.Should().HaveCount(numberItems);

        }

        public class InserirNovosRegistosMultTenantParams : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "http://domain.tenant.one.com", 94 };
                yield return new object[] { "http://domain.tenant.two.com", 1 };
                yield return new object[] { "http://domain.tenant.three.com", 5 };
                yield return new object[] { "http://domain.tenant.four.com", 0 };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class TestTenantInsertParameters : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var tenants = Seed.TenantEntityList();

                yield return new object[]
                {
                    new []
                    {
                        new TestTenantEntity (Guid.Empty, new NomeValueObject("Armand", "Villarreal"), new EmailValueObject("lorem.tristique@posuerevulputatelacus.ca"))
                    },
                    tenants[0].Host,
                    1,
                };
                yield return new object[]
                {
                    new []
                    {
                        new TestTenantEntity (Guid.Empty, new NomeValueObject("Tarik", "Oneal"), new EmailValueObject("semper.pretium.neque@malesuada.net")),
                        new TestTenantEntity (Guid.Empty, new NomeValueObject("Baxter", "Sexton"), new EmailValueObject("lacus@Aliquamgravida.co.uk"))
                    },
                    tenants[1].Host,
                    2
                };
                yield return new object[]
                {
                    new []
                    {
                        new TestTenantEntity (Guid.Empty, new NomeValueObject("Tarik", "Oneal"), new EmailValueObject("semper.pretium.neque@malesuada.net")),
                        new TestTenantEntity (Guid.Empty, new NomeValueObject("Baxter", "Sexton"), new EmailValueObject("lacus@Aliquamgravida.co.uk")),
                        new TestTenantEntity (Guid.Empty, new NomeValueObject("Armand", "Villarreal"), new EmailValueObject("lorem.tristique@posuerevulputatelacus.ca")),
                    },
                    tenants[2].Host,
                    3
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}