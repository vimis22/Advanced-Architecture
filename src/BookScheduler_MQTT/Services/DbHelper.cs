// Services/DbHelper.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using BookScheduler_MQTT.Models;

namespace BookScheduler_MQTT.Services
{
    public class DbHelper
    {
        private readonly string _connStr;

        public DbHelper(string connStr)
        {
            _connStr = connStr;
        }

        private IDbConnection Conn() => new NpgsqlConnection(_connStr);

        // --- Machines ---
        public async Task<IEnumerable<MachineDto>> GetMachinesAsync()
        {
            using var c = Conn();
            var sql = @"SELECT id, name, type, pages_per_min AS PagesPerMin, status, last_seen, metadata
                        FROM machines ORDER BY name";
            return await c.QueryAsync<MachineDto>(sql);
        }

        public async Task<IEnumerable<MachineDto>> GetAvailableMachinesByTypeAsync(string type)
        {
            using var c = Conn();
            var sql = @"SELECT id, name, type, pages_per_min AS PagesPerMin, status, last_seen, metadata
                        FROM machines
                        WHERE type = @type AND status = 'idle'
                        ORDER BY name";
            return await c.QueryAsync<MachineDto>(sql, new { type });
        }

        public async Task SetMachineHeartbeatAsync(Guid machineId, bool isUp)
        {
            using var c = Conn();
            var sql = @"UPDATE machines
                        SET last_seen = now(),
                            status = CASE WHEN status = 'off' AND @isUp = true THEN 'idle' ELSE status END
                        WHERE id = @machineId";
            await c.ExecuteAsync(sql, new { machineId, isUp });
        }

        public async Task SetMachineStatusAsync(Guid machineId, string status)
        {
            using var c = Conn();
            await c.ExecuteAsync("UPDATE machines SET status = @status, last_seen = now() WHERE id = @id",
                new { id = machineId, status });
        }

        public async Task SetMachineBusyAsync(Guid machineId, bool isBusy)
        {
            using var c = Conn();
            var status = isBusy ? "running" : "idle";
            await c.ExecuteAsync("UPDATE machines SET status = @status WHERE id = @id",
                new { id = machineId, status });
        }

        // --- Books ---
        public async Task<IEnumerable<BookDto>> GetAllBooksAsync()
        {
            using var c = Conn();
            var sql = "SELECT id, title, copies, pages, created_at AS CreatedAt FROM books ORDER BY created_at";
            return await c.QueryAsync<BookDto>(sql);
        }

        public async Task<IEnumerable<BookDto>> GetPendingBooksAsync()
        {
            using var c = Conn();
            var sql = "SELECT id, title, copies, pages, created_at AS CreatedAt FROM books ORDER BY created_at";
            return await c.QueryAsync<BookDto>(sql);
        }

        // --- Book stages ---
        public async Task EnsureStageExistsAsync(Guid bookId, string stage)
        {
            using var c = Conn();
            var sql = @"
                INSERT INTO book_stages (book_id, stage)
                SELECT @bookId, @stage
                WHERE NOT EXISTS (
                    SELECT 1 FROM book_stages WHERE book_id=@bookId AND stage=@stage
                )";
            await c.ExecuteAsync(sql, new { bookId, stage });
        }

        public async Task<BookStageDto?> GetBookStageAsync(Guid bookId, string stage)
        {
            using var c = Conn();
            var sql = @"SELECT id, book_id AS BookId, stage, status, assigned_machine AS AssignedMachine, progress, updated_at AS UpdatedAt
                        FROM book_stages
                        WHERE book_id = @bookId AND stage = @stage";
            return await c.QueryFirstOrDefaultAsync<BookStageDto>(sql, new { bookId, stage });
        }

        public async Task<BookStageDto?> GetBookStageByIdAsync(Guid stageId)
        {
            using var c = Conn();
            var sql = @"SELECT id, book_id AS BookId, stage, status, assigned_machine AS AssignedMachine, progress, updated_at AS UpdatedAt
                        FROM book_stages
                        WHERE id = @stageId";
            return await c.QueryFirstOrDefaultAsync<BookStageDto>(sql, new { stageId });
        }

        public async Task<IEnumerable<BookStageDto>> GetStagesForBookAsync(Guid bookId)
        {
            using var c = Conn();
            var sql = @"SELECT id, book_id AS BookId, stage, status, assigned_machine AS AssignedMachine, progress, updated_at AS UpdatedAt
                        FROM book_stages
                        WHERE book_id = @bookId
                        ORDER BY stage";
            return await c.QueryAsync<BookStageDto>(sql, new { bookId });
        }

        public async Task AssignStageMachineAsync(Guid bookId, string stage, Guid machineId)
        {
            using var c = Conn();
            var sql = @"UPDATE book_stages
                        SET assigned_machine = @machineId, status = 'running', updated_at = now()
                        WHERE book_id = @bookId AND stage = @stage";
            await c.ExecuteAsync(sql, new { machineId, bookId, stage });
        }

        public async Task UpdateStageProgressAsync(Guid stageId, int progress, string? status = null)
        {
            using var c = Conn();
            var sql = @"UPDATE book_stages
                        SET progress = @progress,
                            status = COALESCE(@status, status),
                            updated_at = now()
                        WHERE id = @stageId";
            await c.ExecuteAsync(sql, new { progress, status, stageId });
        }

        public async Task SetStageStatusAsync(Guid stageId, string status)
        {
            using var c = Conn();
            await c.ExecuteAsync("UPDATE book_stages SET status = @status, updated_at = now() WHERE id = @stageId",
                new { status, stageId });
        }

        public async Task<string?> GetStageStatusAsync(Guid bookId, string stage)
        {
            using var c = Conn();
            var sql = "SELECT status FROM book_stages WHERE book_id = @bookId AND stage = @stage";
            return await c.ExecuteScalarAsync<string?>(sql, new { bookId, stage });
        }

        // --- Job events ---
        public async Task InsertJobEventAsync(Guid? bookStageId, Guid? machineId, string eventType, object eventData)
        {
            using var c = Conn();
            var sql = @"INSERT INTO job_events (book_stage_id, machine_id, event_type, event_data)
                        VALUES (@bookStageId, @machineId, @eventType, @eventData::jsonb)";
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(eventData ?? new { });
            await c.ExecuteAsync(sql, new { bookStageId, machineId, eventType, eventData = json });
        }

        // --- Machines last seen decay ---
        public async Task<IEnumerable<MachineDto>> GetDownMachinesOlderThanAsync(TimeSpan age)
        {
            using var c = Conn();
            var sql = @"SELECT id, name, type, pages_per_min AS PagesPerMin, status, last_seen, metadata
                        FROM machines
                        WHERE last_seen < now() - (@ageInterval::interval)";

            var ageInterval = $"{(int)age.TotalHours:D2}:{age.Minutes:D2}:{age.Seconds:D2}";
            return await c.QueryAsync<MachineDto>(sql, new { ageInterval });
        }

        // --- Nullable-friendly overloads ---
        public Task SetMachineHeartbeatAsync(Guid? machineId, bool isUp)
        {
            if (!machineId.HasValue) return Task.CompletedTask;
            return SetMachineHeartbeatAsync(machineId.Value, isUp);
        }

        public Task SetMachineBusyAsync(Guid? machineId, bool isBusy)
        {
            if (!machineId.HasValue) return Task.CompletedTask;
            return SetMachineBusyAsync(machineId.Value, isBusy);
        }

        public Task UpdateStageProgressAsync(Guid? stageId, int progress, string? status = null)
        {
            if (!stageId.HasValue) return Task.CompletedTask;
            return UpdateStageProgressAsync(stageId.Value, progress, status);
        }

        public async Task<BookStageDto?> GetBookStageAsync(Guid? bookId, string stage)
        {
            if (!bookId.HasValue) return null;
            return await GetBookStageAsync(bookId.Value, stage);
        }
    }
}
