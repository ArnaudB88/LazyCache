using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Unity;
using Unity.AspNet.Mvc;
using Unity.Lifetime;

namespace LazyCache.UnitTestsAspNet
{
    [TestClass]
    public class HttpContextTests
    {
        private static IUnityContainer _container;

        [TestMethod]
        public async Task FactoryMethodHttpContext_ShouldNotBeLost()
        {
            //Arrange
            //1. create dependency injection container
            Func<PerRequestLifetimeManager> createLifetimeManager = () => new PerRequestLifetimeManager();//Needs httpContext for instantions of registered types in the DI container
            _container = new UnityContainer();

            //register db context with lifetime manager so each http call has it's own dbContext
            _container.RegisterType<IDbContext, DbContext>(createLifetimeManager());

            _container.RegisterInstance<IOptions<MemoryCacheOptions>>(Options.Create(new MemoryCacheOptions()));
            _container.RegisterFactory<ICacheProvider>(c => new MemoryCacheProvider(new MemoryCache(c.Resolve<IOptions<MemoryCacheOptions>>())), new ContainerControlledLifetimeManager());//Singleton provider, new MemoryCache every time
            _container.RegisterSingleton<IAppCache, CachingService>();

            //2. Since this is a unit test and we need a HttpContext, create a fake one
            HttpContext.Current = FakeHttpContext();

            //3. Retrieve cached value needing httpContext
            var appCache = _container.Resolve<IAppCache>();
            try
            {
                var cachedValue = await appCache.GetOrAddAsync("foo",
                    async () => await LongRunningDatabaseCallAsync());

                Assert.AreEqual(DbContext.EXPECTED_VALUE, cachedValue);
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        /// <summary>
        /// Not possible to change the behaviour of this method. Dependency injection resolves must happen inside this method.
        /// </summary>
        /// <returns></returns>
        public async Task<int> LongRunningDatabaseCallAsync()
        {
            var dbContext = _container.Resolve<IDbContext>();
            return await dbContext.GetInfoFromDatabaseAsync();
        }

        public static HttpContext FakeHttpContext()
        {
            var httpRequest = new HttpRequest("", "http://stackoverflow/", "");
            var stringWriter = new StringWriter();
            var httpResponse = new HttpResponse(stringWriter);
            var httpContext = new HttpContext(httpRequest, httpResponse);

            var sessionContainer = new HttpSessionStateContainer("id", new SessionStateItemCollection(),
                                                    new HttpStaticObjectsCollection(), 10, true,
                                                    HttpCookieMode.AutoDetect,
                                                    SessionStateMode.InProc, false);

            httpContext.Items["AspSession"] = typeof(HttpSessionState).GetConstructor(
                                        BindingFlags.NonPublic | BindingFlags.Instance,
                                        null, CallingConventions.Standard,
                                        new[] { typeof(HttpSessionStateContainer) },
                                        null)
                                .Invoke(new object[] { sessionContainer });

            return httpContext;
        }
    }
}
