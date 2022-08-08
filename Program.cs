//#pragma warning disable CA1416
using System;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ConsoleGameEngine;

namespace ConsoleGameEngine
{
	public class Program
	{


		static void Main()
		{
			new FPS().Construct(1600, 900, 4, 4, 30, false);

			//new Test().Construct(1280, 720, 4, 4, 30, false);
		}
	}
	public class Test : Engine
	{

		Sprite spr = new(@"C:\dev\ConsoleApp3\Weapons-Doom-Pistol-h.png");
		Point p = new(100, 100);
		int i = 0;
		public override void Create()
		{
			Console.CursorVisible = false;
		}
		public override void Update()
		{
			Console.Title = $"Console Game Engine - Test - {GetFramerate():F2} fps";
			p.Y = 14 + (int)(Math.Sin(i * 0.1f) * 4f);
			DrawSprite(p.X, p.Y, ref spr, spr.Width, spr.Height);
			i++;
		}
		public override void Render()
		{
			BufferDraw();
			BufferInit();

		}
	}

	public class FPS : Engine
	{
		int mapWidth = 32;
		int mapHeight = 32;
		int scoreWidth = 51;
		int scoreHeight = 7;

		double playerPosX = 9;
		double playerPosY = 9;
		double playerRot = -Math.PI / 2.0;
		double playerFOV = Math.PI / 4.0;
		double renderDis = 20.0;
		double walkSpeed = 3.0;
		string map;
		int targetCount;

		public struct sceneObj
		{
			public double x;
			public double y;
			public double vx;
			public double vy;
			public bool remove;
			public Sprite sprite;
		}

		Sprite fullWall = new(@"Resources\fullWall.png");
		Sprite bullet = new(@"Resources\bullet.png");
		Sprite target = new(@"Resources\target.png");
		Sprite pGun = new(@"Resources\Weapons-Doom-Pistol-h.png");
		//Sprite scoreBoard = new(@"Resources\scoreBoard.png");

		Sprite[] scores = new Sprite[11];

		List<sceneObj> sceneObjs = new();


		double[] depthBuffer;

		public override void Create()
		{
			Framerate = 30;
			Console.CursorVisible = false;

			map += "################################";
			map += "#...............#..............#";
			map += "#..@....#########...@...########";
			map += "#..............##..............#";
			map += "#......##......##......##......#";
			map += "#......##..............##......#";
			map += "#..............##..............#";
			map += "###........@...####............#";
			map += "##.............###.............#";
			map += "#............####............###";
			map += "#..............................#";
			map += "#..............##..........@...#";
			map += "#..............##..............#";
			map += "#...@.......#####...........####";
			map += "#..............................#";
			map += "###..####....########....#######";
			map += "####.####.......######.........#";
			map += "#...............#..............#";
			map += "#.......#########.......##..####";
			map += "#..............##..............#";
			map += "#......##..@...##.......#..@...#";
			map += "#......##......##......##......#";
			map += "#..............##..............#";
			map += "###............####............#";
			map += "##.............###.............#";
			map += "#...@........####............###";
			map += "#..............................#";
			map += "#..............................#";
			map += "#..............##..............#";
			map += "#.....@.....##........@.....####";
			map += "#..............##..............#";
			map += "################################";

			depthBuffer = new double[ScreenWidth];

			for(int i = 0; i < mapHeight; i++)
			{
				for (int j = 0; j < mapWidth; j++)
				{
					if (map[i * mapWidth + j] == '@')
					{
						sceneObjs.Add(new sceneObj { x = j, y = i, vx = 0, vy = 0, remove = false, sprite = target });
					}
				}
			}
			map = map.Replace('@', '.');

			targetCount = sceneObjs.Count(x => x.sprite == target);

			scores[0] = new Sprite(@"Resources\scoreBoard0.png");
			scores[1] = new Sprite(@"Resources\scoreBoard1.png");
			scores[2] = new Sprite(@"Resources\scoreBoard2.png");
			scores[3] = new Sprite(@"Resources\scoreBoard3.png");
			scores[4] = new Sprite(@"Resources\scoreBoard4.png");
			scores[5] = new Sprite(@"Resources\scoreBoard5.png");
			scores[6] = new Sprite(@"Resources\scoreBoard6.png");
			scores[7] = new Sprite(@"Resources\scoreBoard7.png");
			scores[8] = new Sprite(@"Resources\scoreBoard8.png");
			scores[9] = new Sprite(@"Resources\scoreBoard9.png");
			scores[10] = new Sprite(@"Resources\scoreBoard.png");
		}

		public override void Update()
		{
			Console.Title = $"Console Game Engine - Tech Demo - {GetFramerate():F2} fps";


			if (GetKeyState(ConsoleKey.W).Held)
			{
				playerPosX += Math.Sin(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				playerPosY += Math.Cos(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				if (map[(int)playerPosX * mapWidth + (int)playerPosY] == '#')
				{
					playerPosX -= Math.Sin(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
					playerPosY -= Math.Cos(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				}
			}

			if (GetKeyState(ConsoleKey.S).Held)
			{
				playerPosX -= Math.Sin(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				playerPosY -= Math.Cos(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				if (map[(int)playerPosX * mapWidth + (int)playerPosY] == '#')
				{
					playerPosX += Math.Sin(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
					playerPosY += Math.Cos(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				}
			}

			if (GetKeyState(ConsoleKey.A).Held)
			{
				playerPosX -= Math.Cos(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				playerPosY += Math.Sin(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				if (map[(int)playerPosX * mapWidth + (int)playerPosY] == '#')
				{
					playerPosX -= Math.Sin(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
					playerPosY += Math.Cos(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				}
			}

			if (GetKeyState(ConsoleKey.D).Held)
			{
				playerPosX += Math.Cos(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				playerPosY -= Math.Sin(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				if (map[(int)playerPosX * mapWidth + (int)playerPosY] == '#')
				{
					playerPosX -= Math.Cos(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
					playerPosY += Math.Sin(playerRot) * walkSpeed * elapsedTime.TotalSeconds;
				}
			}

			if (GetKeyState(ConsoleKey.Q).Held)
			{
				playerRot -= (walkSpeed * 0.5) * elapsedTime.TotalSeconds;
			}

			if (GetKeyState(ConsoleKey.E).Held)
			{
				playerRot += (walkSpeed * 0.5) * elapsedTime.TotalSeconds;
			}

			if (GetKeyState(0x01).Pressed)
			{
				sceneObj o;
				o.x = playerPosX;
				o.y = playerPosY;
				double noise = ((new Random().NextSingle() / 32767.0) -0.5) * 0.1;
				o.vx = Math.Sin(playerRot + noise) * 8.0;
				o.vy = Math.Cos(playerRot + noise) * 8.0;
				o.sprite = bullet;
				o.remove = false;
				sceneObjs.Add(o);
			}

			for (int x = 0; x < ScreenWidth; x++)
			{
				// For each colum, calculate the projected ray angle into world space
				double rayAngle = (playerRot - playerFOV / 2.0) + ((double)x / (double)ScreenWidth) * playerFOV;

				// Find distance to wall
				double stepSize = 0.01; // Increment size for ray casting, decrease to increase resolution
				double distanceToWall = 0.0;

				bool hitWall = false; // set when ray hits wall block
				bool boundary = false; // set when ray hits the boundary between two wall blocks

				double eyeX = Math.Sin(rayAngle);
				double eyeY = Math.Cos(rayAngle);

				double sampleX = 0.0;

				//bool lit = false;

				while (!hitWall && distanceToWall < renderDis)
				{
					distanceToWall += stepSize;
					int testX = (int)(playerPosX + eyeX * distanceToWall);
					int testY = (int)(playerPosY + eyeY * distanceToWall);

					// Test is ray is out of bounds
					if (testX < 0 || testX >= mapWidth || testY < 0 || testY >= mapHeight)
					{
						hitWall = true;
						distanceToWall = renderDis;
					}
					else
					{
						if (map[testX * mapWidth + testY] == '#')
						{
							// Ray is inbounds so test to see if ray cell is a wall block
							// ray has his wall
							hitWall = true;

							// Determine where ray has hit wall.
							double blockMidX = (double)testX + 0.5;
							double blockMidY = (double)testY + 0.5;

							double testPointX = playerPosX + eyeX * distanceToWall;
							double testPointY = playerPosY + eyeY * distanceToWall;

							double testAngle = Math.Atan2((testPointY - blockMidY), (testPointX - blockMidX));

							if (testAngle >= -Math.PI * 0.25 && testAngle < Math.PI * 0.25)
								sampleX = testPointY - (double)testY;
							if (testAngle >= Math.PI * 0.25 && testAngle < Math.PI * 0.75)
								sampleX = testPointX - (double)testX;
							if (testAngle < -Math.PI * 0.25 && testAngle >= -Math.PI * 0.75)
								sampleX = testPointX - (double)testX;
							if (testAngle >= Math.PI * 0.75 || testAngle < -Math.PI * 0.75)
								sampleX = testPointY - (double)testY;
						}
					}
				}
				// Calculate distance to ceiling and floor
				int ceiling = (int)((double)(ScreenHeight / 2.0) - ScreenHeight / (double)distanceToWall);
				int floor = ScreenHeight - ceiling;

				// Update Depth Buffer
				depthBuffer[x] = distanceToWall;

				for (int y = 0; y < ScreenHeight; y++)
				{
					// Each Row
					if (y <= ceiling)
						SetPixel(x, y, '\u2591', 0x40);
					else if (y > ceiling && y <= floor)
					{
						// Draw Wall
						if (distanceToWall < renderDis)
						{
							double sampleY = ((double)y - (double)ceiling) / ((double)floor - (double)ceiling);
							CharInfo c = SampleSprite(fullWall, sampleX, sampleY);
							SetPixel(x, y, c.UnicodeChar, c.Attributes);
						}
						else SetPixel(x, y, '\u2591', 0x40);
					}
					else
					{ //floor
						SetPixel(x, y, '\u2591', 0x80);
					}
				}
			}

			// Update & Draw Objects
			//sceneObjs.ForEach((obj) =>
			for (int i = 0; i < sceneObjs.Count; i++)
			{
				var obj = sceneObjs[i];
				var obj2 = sceneObjs.LastOrDefault();
				// Update Object Physics
				obj.x += (obj.vx * elapsedTime.TotalSeconds);
				obj.y += (obj.vy * elapsedTime.TotalSeconds);

				// Check if object is inside wall - set flag for removal
				if (map[(int)obj.x * mapWidth + (int)obj.y] == '#')
					obj.remove = true;

				// Remove target if bullet occupies the same space.
				if (obj.sprite == target && obj2.sprite == bullet)
				{
					if ((obj.x >= obj2.x - 0.2 && obj.y >= obj2.y - 0.2) && (obj.x <= obj2.x + 0.2 && obj.y <= obj2.y + 0.2))
						obj.remove = true;
				}
				// Can object be seen? Calculates sight vectors
				double vecX = obj.x - playerPosX;
				double vecY = obj.y - playerPosY;
				double distanceFromPlayer = Math.Sqrt((vecX * vecX) + (vecY * vecY));

				double eyeX = Math.Sin(playerRot);
				double eyeY = Math.Cos(playerRot);

				// Calculate angle between object and player's feet, and player's looking direction
				// to determine if the object is in the players field of view
				double objectAngle = Math.Atan2(eyeY, eyeX) - Math.Atan2(vecY, vecX);
				if (objectAngle < -Math.PI)
					objectAngle += (2.0 * Math.PI);
				if (objectAngle > Math.PI)
					objectAngle -= (2.0 * Math.PI);


				bool inPlayerFOV = Math.Abs(objectAngle) <= playerFOV;

				if (inPlayerFOV && distanceFromPlayer >= 0.5f && distanceFromPlayer < renderDis && !obj.remove)
				{
					double objectCeiling = (double)(ScreenHeight / 2.0) - ScreenHeight / ((double)distanceFromPlayer);
					double objectFloor = ScreenHeight - objectCeiling;
					double objectHeight = (objectFloor - objectCeiling) * 1.15;
					double objectAspectRatio = (double)obj.sprite.Height / (double)obj.sprite.Width;
					double objectWidth = objectHeight / objectAspectRatio;
					double middleOfObject = (0.5 * (objectAngle / (playerFOV / 2.0)) + 1) * (double)ScreenHeight;

					// Draw Object
					for (double y = 0; y < objectHeight; y++)
					{
						for (double x = 0; x < objectWidth; x++)
						{
							double sampleX = x / objectWidth;
							double sampleY = y / objectHeight;
							CharInfo c = SampleSprite(obj.sprite, sampleX, sampleY);
							int objectColumn = (int)(middleOfObject + x - (objectWidth / 2.0));
							if (objectColumn >= 0 && objectColumn < ScreenWidth)
							{
								if (c.Attributes != 0xFF && depthBuffer[objectColumn] >= distanceFromPlayer)
								{
									SetPixel(objectColumn, (int)(objectCeiling + y), c.UnicodeChar, c.Attributes);
									depthBuffer[objectColumn] = distanceFromPlayer;
								}
							}
						}
					}
				}
				sceneObjs[i] = obj;
			}

			// Remove dead objects from object list
			sceneObjs.RemoveAll(x => x.remove == true);
			targetCount = sceneObjs.Count(x => x.sprite == target);
			var temp = scores[targetCount];
			// scale ratio for map and gun.
			double scaleRatio = ScreenWidth * 0.15;
			double scaleHeight = ScreenHeight * 0.1;
			double scaleRatio2 = ScreenWidth * 0.2;
			double scaleRatio3 = scoreWidth * 0.2;
			double scaleRatio4 = scoreHeight * 0.1;

			// Display Map & Player & ScoreBoard
			DrawString(map, 1, 1, mapWidth, mapHeight, scaleRatio, scaleRatio);
			DrawSprite((int)(ScreenWidth - (temp.Width /(temp.Width/scaleRatio2)) - 2), 1,ref temp, scaleRatio2, scaleHeight);
			//DrawString(scores[targetCount], (int)(ScreenWidth - (5 / (5 / scaleRatio3)) - 2), 1, 5, 3, scaleRatio, scaleRatio4);
			DrawSprite((int)((ScreenWidth / 2) - ((pGun.Width / 2) / (pGun.Width / scaleRatio2))), (int)(ScreenHeight - (pGun.Height / (pGun.Height / scaleRatio2))),ref pGun, scaleRatio2, scaleRatio2, 5);
			SetPixel(1 + (int)(playerPosY / (mapWidth / scaleRatio)), 1 + (int)(playerPosX / (mapWidth / scaleRatio)), '\u2593', 0x04);
		}

		public override void Render()
		{
			BufferDraw();
		}
	}
}
