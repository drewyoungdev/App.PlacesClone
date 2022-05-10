using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace PlaceApi.Controllers
{
    /// <summary>
    /// Endpoints to interact with redis bitfield which stores the canvas.
    /// Colors within the canvas will be stored as integers defined by the INT_BIT_SIZE.
    /// Size of the canvas will be determined by CANVAS WIDTH * CANVAS_HEIGHT
    /// Bitfield will be stored under KEY
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PlacesController : ControllerBase
    {
        private IDatabase redis;
        private const int CANVAS_WIDTH = 2;
        private const int CANVAS_HEIGHT = 2;
        private const string INT_BIT_SIZE = "u4";
        private const string KEY = "canvas";

        private Random random = new Random();

        public PlacesController(IConnectionMultiplexer connection)
        {
            this.redis = connection.GetDatabase();
        }

        /// <summary>
        /// Resets the canvas
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> ResetBitField()
        {
            this.redis.KeyDelete(KEY);

            for (int x = 0; x < CANVAS_WIDTH; x++)
            {
                for (int y = 0; y < CANVAS_HEIGHT; y++)
                {
                    var n = random.Next(0, 15);

                    Console.WriteLine($"Coordinate ({x}, {y}): {n}");

                    await this.DrawTile(x, y, n);
                }
            }

            return this.Ok();
        }

        /// <summary>
        /// Updates the pixel at a given location offset in the bitfield
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <param name="value">The value</param>
        /// <returns></returns>
        [HttpPost("draw")]
        public async Task<ActionResult> Draw(int x, int y, byte value)
        {
            // TODO: This could be invalid if we ever changed the size of the integer. 15 is the max for 4-bit integer.
            if (value > 15)
                return this.BadRequest($"Invalid value. Value must be {INT_BIT_SIZE}-bit integer");

            bool invalidXCoordinate = x > CANVAS_WIDTH - 1;
            bool invalidYCoordinate = y > CANVAS_HEIGHT - 1;

            if (invalidXCoordinate || invalidYCoordinate)
                return this.BadRequest(
                    "Invalid coordinates: " +
                    $"{(invalidXCoordinate ? $"X coordinate '{x}' exceeds the bounds of '0' -> '{CANVAS_WIDTH - 1}'. " : null)}" +
                    $"{(invalidYCoordinate ? $"Y coordinate '{y}' exceeds the bounds of '0' -> '{CANVAS_HEIGHT - 1}'. " : null)}");

            await this.DrawTile(x, y, value);

            return this.Ok();
        }

        /// <summary>
        /// https://redis.io/commands/bitfield/
        /// Note, if the offset is prefixed with a # character, the specified offset is multiplied by the integer encoding's width.
        /// </summary>
        /// <param name="x">The x coordinate to calculate the offset.</param>
        /// <param name="y">The y coordinate to calculate the offset.</param>
        /// <param name="value">The integer value to set.</param>
        /// <returns></returns>
        private async Task DrawTile(int x, int y, int value)
        {
            var offset = (2 * y) + x;

            await this.redis.ExecuteAsync("BITFIELD", (RedisKey)KEY, "SET", INT_BIT_SIZE, $"#{offset}", value);
        }

        /// <summary>
        /// Retrieves entire bitfield from position 0 -> CANVAS_WIDTH + 1 (+1 because we are including 0 position)
        /// If Content-Type application/octet-stream is requested return
        /// Else default to Base64 encoded byte array.
        /// GETRANGE <KEY> <start> <end>
        /// https://cryptii.com/pipes/base64-to-binary Tool can view base64 as binary and as 4 bit / 8 bit groups
        /// Examples below on how to split 8-bits into 4-bit integers:
        /// https://stackoverflow.com/questions/55133430/how-to-split-a-byte-into-2-parts-and-read-it
        /// https://pyra-handheld.com/boards/threads/splitting-an-8bit-variable-into-two-4bit.23388/
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetBitField()
        {
            RedisResult result = await this.redis.ExecuteAsync("GETRANGE", (RedisKey)KEY, "0", CANVAS_WIDTH + 1);

            byte[] byteArray = (RedisValue)result;

            if (Request.Headers.TryGetValue("Content-Type", out var contentType) && string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
                return this.File(byteArray, "application/octet-stream");

            return this.Ok(byteArray);
        }
    }
}
