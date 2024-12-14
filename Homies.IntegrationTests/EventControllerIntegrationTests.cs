using Homies.Controllers;
using Homies.Data;
using Homies.Data.Models;
using Homies.Models.Event;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Type = Homies.Data.Models.Type;

namespace Homies.Tests
{
    [TestFixture]
    public class EventControllerIntegrationTests
    {
        private HomiesDbContext _dbContext;
        private EventController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<HomiesDbContext>()
                .UseInMemoryDatabase(databaseName: "HomiesDbTest")
                .Options;

            _dbContext = new HomiesDbContext(options);

            _dbContext.Types.AddRange(new List<Type>
            {
                new Type { Id = 1, Name = "Animals" },
                new Type { Id = 2, Name = "Fun" }
            });

            _dbContext.SaveChanges();

            _controller = new EventController(_dbContext);

            var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId"),
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = mockUser }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task AddEvent_ShouldInsertEventIntoDatabase()
        {
            var eventModel = new EventFormModel
            {
                Name = "Test Event",
                Description = "This is a test event",
                TypeId = 1,
                Start = DateTime.Now.AddDays(1),
                End = DateTime.Now.AddDays(2)
            };

            var result = await _controller.Add(eventModel);

            var redirectToActionResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectToActionResult);
            Assert.AreEqual("All", redirectToActionResult.ActionName);

            var eventsInDb = _dbContext.Events.ToList();
            Assert.AreEqual(1, eventsInDb.Count);
            Assert.AreEqual("Test Event", eventsInDb[0].Name);
        }

        [Test]
        public async Task EditEvent_ShouldUpdateEventInDatabase()
        {
            var eventEntity = new Event
            {
                Id = 1,
                Name = "Original Event",
                Description = "Original Description",
                TypeId = 1,
                Start = DateTime.Now.AddDays(1),
                End = DateTime.Now.AddDays(2),
                OrganiserId = "testUserId"
            };

            _dbContext.Events.Add(eventEntity);
            await _dbContext.SaveChangesAsync();

            var eventModel = new EventFormModel
            {
                Name = "Edited Event",
                Description = "Edited Description",
                TypeId = 1,
                Start = DateTime.Now.AddDays(3),
                End = DateTime.Now.AddDays(4)
            };

            var result = await _controller.Edit(eventEntity.Id, eventModel);

            var redirectToActionResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectToActionResult);
            Assert.AreEqual("All", redirectToActionResult.ActionName);

            var updatedEvent = await _dbContext.Events.FindAsync(eventEntity.Id);
            Assert.AreEqual("Edited Event", updatedEvent.Name);
            Assert.AreEqual("Edited Description", updatedEvent.Description);
        }

        [Test]
        public async Task JoinEvent_ShouldAddParticipantToDatabase()
        {
            var eventEntity = new Event
            {
                Id = 1,
                Name = "Joinable Event",
                Description = "Test Join",
                TypeId = 1,
                Start = DateTime.Now.AddDays(1),
                End = DateTime.Now.AddDays(2),
                OrganiserId = "testUserId"
            };

            _dbContext.Events.Add(eventEntity);
            await _dbContext.SaveChangesAsync();

            var result = await _controller.Join(eventEntity.Id);

            var redirectToActionResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectToActionResult);
            Assert.AreEqual("Joined", redirectToActionResult.ActionName);

            var participants = _dbContext.EventsParticipants.ToList();
            Assert.AreEqual(1, participants.Count);
            Assert.AreEqual(eventEntity.Id, participants[0].EventId);
            Assert.AreEqual("testUserId", participants[0].HelperId);
        }

        [Test]
        public async Task LeaveEvent_ShouldRemoveParticipantFromDatabase()
        {
            var eventEntity = new Event
            {
                Id = 1,
                Name = "Leavable Event",
                Description = "Test Leave",
                TypeId = 1,
                Start = DateTime.Now.AddDays(1),
                End = DateTime.Now.AddDays(2),
                OrganiserId = "testUserId"
            };

            var participant = new EventParticipant
            {
                EventId = 1,
                HelperId = "testUserId"
            };

            _dbContext.Events.Add(eventEntity);
            _dbContext.EventsParticipants.Add(participant);
            await _dbContext.SaveChangesAsync();

            var result = await _controller.Leave(eventEntity.Id);

            var redirectToActionResult = result as RedirectToActionResult;
            Assert.IsNotNull(redirectToActionResult);
            Assert.AreEqual("All", redirectToActionResult.ActionName);

            var participants = _dbContext.EventsParticipants.ToList();
            Assert.AreEqual(0, participants.Count);
        }

        [Test]
        public async Task Joined_ShouldReturnUserJoinedEvents()
        {
            var eventEntity = new Event
            {
                Id = 1,
                Name = "Joined Event",
                Description = "User joined event",
                TypeId = 1,
                Start = DateTime.Now.AddDays(1),
                End = DateTime.Now.AddDays(2),
                OrganiserId = "testUserId"
            };

            var participant = new EventParticipant
            {
                EventId = 1,
                HelperId = "testUserId"
            };

            _dbContext.Events.Add(eventEntity);
            _dbContext.EventsParticipants.Add(participant);
            await _dbContext.SaveChangesAsync();

            var result = await _controller.Joined();

            var viewResult = result as ViewResult;
            Assert.IsNotNull(viewResult);

            var model = viewResult.Model as List<EventViewShortModel>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
            Assert.AreEqual("Joined Event", model[0].Name);
        }
    }
}
