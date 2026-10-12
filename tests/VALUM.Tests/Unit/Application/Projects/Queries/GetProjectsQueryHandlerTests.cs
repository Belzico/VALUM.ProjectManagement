using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using VALUM.Application.Common.Pagination;
using VALUM.Application.Projects.DTOs;
using VALUM.Application.Projects.Queries;
using VALUM.Application.Projects;
using VALUM.Domain.Entities;
using VALUM.Domain.Enums;
using Xunit;

namespace VALUM.Tests.Unit.Application.Projects.Queries
{
    public class GetProjectsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsEntitiesToDtosAndCalculatesPagination()
        {
            // Arrange
            var mockRepo = new Mock<IProjectRepository>();
            
            var projects = new List<Project>
            {
                new Project { Id = Guid.NewGuid(), Name = "Project 1", Status = ProjectStatus.Active, CreatedAt = DateTime.UtcNow, OwnerId = Guid.NewGuid() },
                new Project { Id = Guid.NewGuid(), Name = "Project 2", Status = ProjectStatus.Active, CreatedAt = DateTime.UtcNow, OwnerId = Guid.NewGuid() }
            };
            
            mockRepo.Setup(r => r.CountAsync(It.IsAny<ProjectStatus?>())).ReturnsAsync(2);
            mockRepo.Setup(r => r.GetPageAsync(It.IsAny<ProjectStatus?>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(projects);
            
            var handler = new GetProjectsQueryHandler(mockRepo.Object);
            var query = new GetProjectsQuery { Page = 1, PageSize = 10 };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(2, result.Items.Count);
            
            var firstDto = result.Items.First();
            Assert.Equal(projects[0].Id, firstDto.Id);
            Assert.Equal("Project 1", firstDto.Name);
            Assert.Equal(ProjectStatus.Active, firstDto.Status);
        }

        [Fact]
        public async Task Handle_WithStatusFilter_FiltersCorrectly()
        {
            // Arrange
            var mockRepo = new Mock<IProjectRepository>();
            
            var projects = new List<Project>
            {
                new Project { Id = Guid.NewGuid(), Name = "Archived Project", Status = ProjectStatus.Archived, CreatedAt = DateTime.UtcNow, OwnerId = Guid.NewGuid() }
            };
            
            mockRepo.Setup(r => r.CountAsync(ProjectStatus.Archived)).ReturnsAsync(1);
            mockRepo.Setup(r => r.GetPageAsync(ProjectStatus.Archived, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(projects);
            
            var handler = new GetProjectsQueryHandler(mockRepo.Object);
            var query = new GetProjectsQuery { Page = 1, PageSize = 10, Status = ProjectStatus.Archived };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal(ProjectStatus.Archived, result.Items.First().Status);
        }
    }
}
