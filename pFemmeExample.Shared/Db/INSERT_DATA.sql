USE [db_pfemme]
GO

SET IDENTITY_INSERT [dbuser_pfemme].[Cycles] ON 
GO

-- ====================================================================
-- ISTORIJSKI PODACI (MAJ 2024 - MAJ 2026) - SKALA 0 DO 2
-- Generisano sa prirodnim biološkim varijacijama u dužini i simptomima
-- ====================================================================

-- --- GODINA 2024 ---

-- Maj 2024 (Ciklus 1 - Dužina krvarenja: 5 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(101, N'REAL20240518', N'T0012233009276130606245230499247964', CAST(N'2024-05-18T08:30:00.000' AS DateTime), N'Periode hat begonnen, starke Unterleibsschmerzen.', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1716013800),
(102, N'REAL20240519', N'T0012233009276130606245230499247964', CAST(N'2024-05-19T09:15:00.000' AS DateTime), N'Starke Blutung, nehme Ibuprofen.', 1, 2, 2, 2, 2, 0, 2, GETDATE(), GETDATE(), 1716100500),
(103, N'REAL20240520', N'T0012233009276130606245230499247964', CAST(N'2024-05-20T11:00:00.000' AS DateTime), N'Lässt etwas nach, immer noch müde.', 1, 2, 1, 1, 2, 0, 1, GETDATE(), GETDATE(), 1716187200),
(104, N'REAL20240521', N'T0012233009276130606245230499247964', CAST(N'2024-05-21T08:00:00.000' AS DateTime), N'Nur noch leichte Schmierblutung.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1716273600),
(105, N'REAL20240522', N'T0012233009276130606245230499247964', CAST(N'2024-05-22T20:00:00.000' AS DateTime), N'Vorbei.', 1, 0, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1716316800),
-- Ovulacija (Sredina ciklusa)
(106, N'REAL20240601', N'T0012233009276130606245230499247964', CAST(N'2024-06-01T14:00:00.000' AS DateTime), N'Mittelschmerz auf der rechten Seite, Ovulationstest positiv.', 0, 0, 1, 0, 0, 0, 1, GETDATE(), GETDATE(), 1717250400);

-- Jun 2024 (Ciklus nakon 29 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(107, N'REAL20240615', N'T0012233009276130606245230499247964', CAST(N'2024-06-15T22:30:00.000' AS DateTime), N'Kam abends überraschend.', 1, 2, 2, 0, 1, 0, 2, GETDATE(), GETDATE(), 1718490600),
(108, N'REAL20240616', N'T0012233009276130606245230499247964', CAST(N'2024-06-16T09:00:00.000' AS DateTime), N'Tag 2, sehr intensiv.', 1, 2, 2, 1, 2, 1, 2, GETDATE(), GETDATE(), 1718528400),
(109, N'REAL20240617', N'T0012233009276130606245230499247964', CAST(N'2024-06-17T10:00:00.000' AS DateTime), N'Besserung der Schmerzen.', 1, 2, 1, 0, 2, 0, 1, GETDATE(), GETDATE(), 1718614800),
(110, N'REAL20240618', N'T0012233009276130606245230499247964', CAST(N'2024-06-18T08:00:00.000' AS DateTime), N'Fast vorbei.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1718701200);

-- Jul 2024 (Ciklus nakon 27 dana - Kraći ciklus zbog stresa/putovanja)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(111, N'REAL20240712', N'T0012233009276130606245230499247964', CAST(N'2024-07-12T07:00:00.000' AS DateTime), N'Etwas früher diesen Monat (Urlaubstress).', 1, 2, 1, 2, 2, 0, 2, GETDATE(), GETDATE(), 1720760400),
(112, N'REAL20240713', N'T0012233009276130606245230499247964', CAST(N'2024-07-13T09:00:00.000' AS DateTime), N'Kopfschmerzen wegen der Hitze.', 1, 2, 2, 2, 2, 1, 2, GETDATE(), GETDATE(), 1720851600),
(113, N'REAL20240714', N'T0012233009276130606245230499247964', CAST(N'2024-07-14T11:00:00.000' AS DateTime), N'Wassereinlagerungen lassen nach.', 1, 1, 0, 1, 1, 0, 0, GETDATE(), GETDATE(), 1720954800),
(114, N'REAL20240715', N'T0012233009276130606245230499247964', CAST(N'2024-07-15T08:00:00.000' AS DateTime), N'Schmierblutung am Ende.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1721030400);

-- Avgust 2024 (Ciklus nakon 31 dan - Duži ciklus)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(115, N'REAL20240810', N'T0012233009276130606245230499247964', CAST(N'2024-08-12T06:30:00.000' AS DateTime), N'Zyklus hat sich diesmal verspätet.', 1, 2, 2, 1, 1, 0, 2, GETDATE(), GETDATE(), 1723444200),
(116, N'REAL20240811', N'T0012233009276130606245230499247964', CAST(N'2024-08-13T09:00:00.000' AS DateTime), N'Sehr starke Krämpfe.', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1723539600),
(117, N'REAL20240812', N'T0012233009276130606245230499247964', CAST(N'2024-08-14T10:00:00.000' AS DateTime), N'Blutung normalisiert sich.', 1, 2, 1, 0, 1, 0, 1, GETDATE(), GETDATE(), 1723626000),
(118, N'REAL20240813', N'T0012233009276130606245230499247964', CAST(N'2024-08-15T08:00:00.000' AS DateTime), N'Minimal.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1723712400);

-- Septembar 2024 (Ciklus nakon 28 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(119, N'REAL20240909', N'T0012233009276130606245230499247964', CAST(N'2024-09-09T08:00:00.000' AS DateTime), N'Pünktlich. Klassischer Verlauf.', 1, 2, 2, 0, 2, 0, 2, GETDATE(), GETDATE(), 1725861600),
(120, N'REAL20240910', N'T0012233009276130606245230499247964', CAST(N'2024-09-10T09:00:00.000' AS DateTime), N'Migräne am Nachmittag.', 1, 2, 2, 2, 2, 1, 2, GETDATE(), GETDATE(), 1725951600),
(121, N'REAL20240911', N'T0012233009276130606245230499247964', CAST(N'2024-09-11T12:00:00.000' AS DateTime), N'Migräne ist weg, Blutung schwächer.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1726056000),
(122, N'REAL20240912', N'T0012233009276130606245230499247964', CAST(N'2024-09-12T08:00:00.000' AS DateTime), N'Sauber.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1726124400);

-- Oktobar 2024 (Ciklus nakon 28 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(123, N'REAL20241005', N'T0012233009276130606245230499247964', CAST(N'2024-10-05T18:00:00.000' AS DateTime), N'PMS - Sehr gereizt, Heißhunger auf Süßes, kein Blut.', 0, 0, 0, 1, 2, 0, 0, GETDATE(), GETDATE(), 1728151200),
(124, N'REAL20241007', N'T0012233009276130606245230499247964', CAST(N'2024-10-07T06:00:00.000' AS DateTime), N'Periode hat begonnen.', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1728271200),
(125, N'REAL20241008', N'T0012233009276130606245230499247964', CAST(N'2024-10-08T09:00:00.000' AS DateTime), N'Rückenschmerzen extrem.', 1, 2, 2, 0, 2, 0, 2, GETDATE(), GETDATE(), 1728370800),
(126, N'REAL20241009', N'T0012233009276130606245230499247964', CAST(N'2024-10-09T14:00:00.000' AS DateTime), N'Besser.', 1, 1, 1, 0, 1, 0, 1, GETDATE(), GETDATE(), 1728482400),
(127, N'REAL20241010', N'T0012233009276130606245230499247964', CAST(N'2024-10-10T08:00:00.000' AS DateTime), N'Ende.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1728547200);

-- Novembar 2024 (Ciklus nakon 26 dana - Kraći ciklus)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(128, N'REAL20241102', N'T0012233009276130606245230499247964', CAST(N'2024-11-02T08:00:00.000' AS DateTime), N'Diesmal nur 26 Tage Zykluslänge.', 1, 2, 2, 0, 1, 0, 2, GETDATE(), GETDATE(), 1730530800),
(129, N'REAL20241103', N'T0012233009276130606245230499247964', CAST(N'2024-11-03T09:00:00.000' AS DateTime), N'Normaler Verlauf.', 1, 2, 1, 0, 2, 0, 1, GETDATE(), GETDATE(), 1730620800),
(130, N'REAL20241104', N'T0012233009276130606245230499247964', CAST(N'2024-11-04T10:00:00.000' AS DateTime), N'Wenig Blutung.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1730707200),
(131, N'REAL20241105', N'T0012233009276130606245230499247964', CAST(N'2024-11-05T08:00:00.000' AS DateTime), N'Vorbei.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1730793600);

-- Decembar 2024 (Ciklus nakon 30 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(132, N'REAL20241202', N'T0012233009276130606245230499247964', CAST(N'2024-12-02T07:15:00.000' AS DateTime), N'Winterblues, sehr müde am ersten Tag.', 1, 2, 2, 2, 2, 1, 2, GETDATE(), GETDATE(), 1733123700),
(133, N'REAL20241203', N'T0012233009276130606245230499247964', CAST(N'2024-12-03T09:00:00.000' AS DateTime), N'Krämpfe lassen nach.', 1, 2, 1, 1, 2, 0, 1, GETDATE(), GETDATE(), 1733212800),
(134, N'REAL20241204', N'T0012233009276130606245230499247964', CAST(N'2024-12-04T11:00:00.000' AS DateTime), N'Leicht.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1733306400),
(135, N'REAL20241205', N'T0012233009276130606245230499247964', CAST(N'2024-12-05T08:00:00.000' AS DateTime), N'Fertig.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1733385600);


-- --- GODINA 2025 ---

-- Januar 2025 (Ciklus nakon 29 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(136, N'REAL20250101', N'T0012233009276130606245230499247964', CAST(N'2025-01-01T10:00:00.000' AS DateTime), N'Neujahr startet mit der Periode.', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1735722000),
(137, N'REAL20250102', N'T0012233009276130606245230499247964', CAST(N'2025-01-02T09:00:00.000' AS DateTime), N'Kopfschmerzen.', 1, 2, 1, 2, 1, 0, 2, GETDATE(), GETDATE(), 1735808400),
(138, N'REAL20250103', N'T0012233009276130606245230499247964', CAST(N'2025-01-03T13:00:00.000' AS DateTime), N'Wird schwächer.', 1, 1, 0, 0, 1, 0, 1, GETDATE(), GETDATE(), 1735909200),
(139, N'REAL20250104', N'T0012233009276130606245230499247964', CAST(N'2025-01-04T08:00:00.000' AS DateTime), N'Letzter Tag.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1735977600);

-- Februar 2025 (Ciklus nakon 28 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(140, N'REAL20250129', N'T0012233009276130606245230499247964', CAST(N'2025-01-29T07:30:00.000' AS DateTime), N'Pünktlich am 29. Tag des Zyklus.', 1, 2, 2, 0, 2, 0, 2, GETDATE(), GETDATE(), 1738132200),
(141, N'REAL20250130', N'T0012233009276130606245230499247964', CAST(N'2025-01-30T09:00:00.000' AS DateTime), N'Starke Müdigkeit.', 1, 2, 1, 1, 2, 0, 2, GETDATE(), GETDATE(), 1738221600),
(142, N'REAL20250131', N'T0012233009276130606245230499247964', CAST(N'2025-01-31T11:00:00.000' AS DateTime), N'Kaum noch Schmerzen.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1738311600),
(143, N'REAL20250201', N'T0012233009276130606245230499247964', CAST(N'2025-02-01T08:00:00.000' AS DateTime), N'Ende.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1738396800);

-- Mart 2025 (Ciklus nakon 27 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(144, N'REAL20250225', N'T0012233009276130606245230499247964', CAST(N'2025-02-25T06:00:00.000' AS DateTime), N'Starke Migräne direkt am Anfang.', 1, 2, 2, 2, 2, 1, 2, GETDATE(), GETDATE(), 1740462000),
(145, N'REAL20250226', N'T0012233009276130606245230499247964', CAST(N'2025-02-26T09:00:00.000' AS DateTime), N'Migräne lässt nach, Unterleib zieht.', 1, 2, 2, 1, 1, 0, 2, GETDATE(), GETDATE(), 1740550800),
(146, N'REAL20250227', N'T0012233009276130606245230499247964', CAST(N'2025-02-27T12:00:00.000' AS DateTime), N'Normalisierung.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1740654000),
(147, N'REAL20250228', N'T0012233009276130606245230499247964', CAST(N'2025-02-28T08:00:00.000' AS DateTime), N'Vorbei.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1740729600);

-- April 2025 (Ciklus nakon 30 dana - Malo kasni)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(148, N'REAL20250327', N'T0012233009276130606245230499247964', CAST(N'2025-03-27T08:00:00.000' AS DateTime), N'Ganz normaler Verlauf.', 1, 2, 1, 0, 1, 0, 2, GETDATE(), GETDATE(), 1743058800),
(149, N'REAL20250328', N'T0012233009276130606245230499247964', CAST(N'2025-03-28T09:30:00.000' AS DateTime), N'Tag 2.', 1, 2, 1, 0, 2, 0, 1, GETDATE(), GETDATE(), 1743147000),
(150, N'REAL20250329', N'T0012233009276130606245230499247964', CAST(N'2025-03-29T11:00:00.000' AS DateTime), N'Schmierblutung.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1743242400),
(151, N'REAL20250330', N'T0012233009276130606245230499247964', CAST(N'2025-03-30T08:00:00.000' AS DateTime), N'Sauber.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1743321600);

-- Maj 2025 (Ciklus nakon 28 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(152, N'REAL20250424', N'T0012233009276130606245230499247964', CAST(N'2025-04-24T07:00:00.000' AS DateTime), N'Frühlingsstart, Unterleibsschmerzen.', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1745470800),
(153, N'REAL20250425', N'T0012233009276130606245230499247964', CAST(N'2025-04-25T09:00:00.000' AS DateTime), N'Sehr starke Krämpfe am zweiten Tag.', 1, 2, 2, 0, 1, 0, 2, GETDATE(), GETDATE(), 1745564400),
(154, N'REAL20250426', N'T0012233009276130606245230499247964', CAST(N'2025-04-26T12:00:00.000' AS DateTime), N'Lässt nach.', 1, 1, 1, 0, 0, 0, 1, GETDATE(), GETDATE(), 1745668800),
(155, N'REAL20250427', N'T0012233009276130606245230499247964', CAST(N'2025-04-27T08:00:00.000' AS DateTime), N'Vorbei.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1745740800);

-- Jun 2025 (Ciklus nakon 28 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(156, N'REAL20250522', N'T0012233009276130606245230499247964', CAST(N'2025-05-22T08:15:00.000' AS DateTime), N'Ziemlich pünktlich.', 1, 2, 1, 0, 1, 0, 2, GETDATE(), GETDATE(), 1747894500),
(157, N'REAL20250523', N'T0012233009276130606245230499247964', CAST(N'2025-05-23T09:00:00.000' AS DateTime), N'Normaler Tag, mäßige Blutung.', 1, 2, 1, 0, 1, 0, 1, GETDATE(), GETDATE(), 1747981200),
(158, N'REAL20250524', N'T0012233009276130606245230499247964', CAST(N'2025-05-24T11:00:00.000' AS DateTime), N'Kaum noch etwas.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1748074800),
(159, N'REAL20250525', N'T0012233009276130606245230499247964', CAST(N'2025-05-25T08:00:00.000' AS DateTime), N'Ende.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1748160000);

-- Jul 2025 (Ciklus nakon 29 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(160, N'REAL20250620', N'T0012233009276130606245230499247964', CAST(N'2025-06-20T06:45:00.000' AS DateTime), N'Sommerzyklus beginnt.', 1, 2, 1, 1, 2, 1, 2, GETDATE(), GETDATE(), 1750394700),
(161, N'REAL20250621', N'T0012233009276130606245230499247964', CAST(N'2025-06-21T09:00:00.000' AS DateTime), N'Heiß draußen, Kreislaufprobleme und Müdigkeit.', 1, 2, 2, 2, 2, 1, 2, GETDATE(), GETDATE(), 1750482000),
(162, N'REAL20250622', N'T0012233009276130606245230499247964', CAST(N'2025-06-22T13:00:00.000' AS DateTime), N'Besser, Blutung nimmt ab.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1750587600),
(163, N'REAL20250623', N'T0012233009276130606245230499247964', CAST(N'2025-06-23T08:00:00.000' AS DateTime), N'Ausgelaufen.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1750665600);

-- Avgust 2025 (Ciklus nakon 27 dana - Malo kraći zbog vrućina)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(164, N'REAL20250717', N'T0012233009276130606245230499247964', CAST(N'2025-07-17T08:00:00.000' AS DateTime), N'Kommt etwas früher.', 1, 2, 2, 0, 1, 0, 2, GETDATE(), GETDATE(), 1752739200),
(165, N'REAL20250718', N'T0012233009276130606245230499247964', CAST(N'2025-07-18T09:00:00.000' AS DateTime), N'Keine großen Besonderheiten.', 1, 2, 1, 0, 1, 0, 1, GETDATE(), GETDATE(), 1752825600),
(166, N'REAL20250719', N'T0012233009276130606245230499247964', CAST(N'2025-07-19T10:00:00.000' AS DateTime), N'Sehr schwach heute.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1752912000),
(167, N'REAL20250720', N'T0012233009276130606245230499247964', CAST(N'2025-07-20T08:00:00.000' AS DateTime), N'Vorbei.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1752998400);

-- Septembar 2025 (Ciklus nakon 30 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(168, N'REAL20250816', N'T0012233009276130606245230499247964', CAST(N'2025-08-16T08:00:00.000' AS DateTime), N'Muster verlagert sich leicht nach hinten.', 1, 2, 1, 1, 1, 0, 2, GETDATE(), GETDATE(), 1755331200),
(169, N'REAL20250817', N'T0012233009276130606245230499247964', CAST(N'2025-08-17T09:00:00.000' AS DateTime), N'Leichte Übelkeit am Morgen.', 1, 2, 1, 0, 2, 2, 1, GETDATE(), GETDATE(), 1755417600),
(170, N'REAL20250818', N'T0012233009276130606245230499247964', CAST(N'2025-08-18T12:00:00.000' AS DateTime), N'Abklingend.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1755518400),
(171, N'REAL20250819', N'T0012233009276130606245230499247964', CAST(N'2025-08-19T08:00:00.000' AS DateTime), N'Fertig.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1755590400);

-- Oktobar 2025 (Ciklus nakon 28 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(172, N'REAL20250913', N'T0012233009276130606245230499247964', CAST(N'2025-09-13T07:30:00.000' AS DateTime), N'Herbstbeginn, normale Periode.', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1757748600),
(173, N'REAL20250914', N'T0012233009276130606245230499247964', CAST(N'2025-09-14T09:30:00.000' AS DateTime), N'Rückenschmerzen und Ziehen im Unterleib.', 1, 2, 2, 0, 1, 0, 2, GETDATE(), GETDATE(), 1757835000),
(174, N'REAL20250915', N'T0012233009276130606245230499247964', CAST(N'2025-09-15T11:00:00.000' AS DateTime), N'Kaum noch Schmerzen, Blutung wird weniger.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1757924400),
(175, N'REAL20250916', N'T0012233009276130606245230499247964', CAST(N'2025-09-16T08:00:00.000' AS DateTime), N'Ende.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1758009600);

-- Novembar 2025 (Ciklus nakon 29 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(176, N'REAL20251012', N'T0012233009276130606245230499247964', CAST(N'2025-10-12T08:00:00.000' AS DateTime), N'Normaler Start.', 1, 2, 1, 0, 1, 0, 2, GETDATE(), GETDATE(), 1760256000),
(177, N'REAL20251013', N'T0012233009276130606245230499247964', CAST(N'2025-10-13T09:00:00.000' AS DateTime), N'Sehr müde heute.', 1, 2, 1, 0, 2, 0, 1, GETDATE(), GETDATE(), 1760342400),
(178, N'REAL20251014', N'T0012233009276130606245230499247964', CAST(N'2025-10-14T10:00:00.000' AS DateTime), N'Schon fast vorbei.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1760428800);

-- Decembar 2025 (Ciklus nakon 27 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(179, N'REAL20251108', N'T0012233009276130606245230499247964', CAST(N'2025-11-08T07:00:00.000' AS DateTime), N'Verschiebt sich leicht nach vorne.', 1, 2, 2, 2, 2, 0, 2, GETDATE(), GETDATE(), 1762588800),
(180, N'REAL20251109', N'T0012233009276130606245230499247964', CAST(N'2025-11-09T09:00:00.000' AS DateTime), N'Viel Schlaf gebraucht, Wärmekissen hilft.', 1, 2, 1, 1, 2, 0, 2, GETDATE(), GETDATE(), 1762675200),
(181, N'REAL20251110', N'T0012233009276130606245230499247964', CAST(N'2025-11-10T12:00:00.000' AS DateTime), N'Leichte Schmerzen abklingend.', 1, 1, 1, 0, 1, 0, 1, GETDATE(), GETDATE(), 1762776000),
(182, N'REAL20251111', N'T0012233009276130606245230499247964', CAST(N'2025-11-11T08:00:00.000' AS DateTime), N'Fertig.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1762848000);


-- --- GODINA 2026 ---

-- Januar 2026 (Ciklus nakon 31 dan - Kasni zbog praznika i stresa)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(183, N'REAL20260109', N'T0012233009276130606245230499247964', CAST(N'2026-01-09T08:30:00.000' AS DateTime), N'Neues Jahr, verspäteter Rhythmus (Weihnachtsstress).', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1767947400),
(184, N'REAL20260110', N'T0012233009276130606245230499247964', CAST(N'2026-01-10T09:00:00.000' AS DateTime), N'Krämpfe lassen etwas nach.', 1, 2, 1, 0, 2, 0, 1, GETDATE(), GETDATE(), 1768035600),
(185, N'REAL20260111', N'T0012233009276130606245230499247964', CAST(N'2026-01-11T11:00:00.000' AS DateTime), N'Schwache Blutung.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1768129200),
(186, N'REAL20260112', N'T0012233009276130606245230499247964', CAST(N'2026-01-12T08:00:00.000' AS DateTime), N'Ende.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1768204800);

-- Februar 2026 (Ciklus nakon 27 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(187, N'REAL20260205', N'T0012233009276130606245230499247964', CAST(N'2026-02-05T07:00:00.000' AS DateTime), N'Wieder ein kürzerer Zyklus.', 1, 2, 2, 1, 2, 1, 2, GETDATE(), GETDATE(), 1770274800),
(188, N'REAL20260206', N'T0012233009276130606245230499247964', CAST(N'2026-02-06T09:00:00.000' AS DateTime), N'Müdigkeit intensiv, mag nichts essen.', 1, 2, 1, 1, 2, 1, 1, GETDATE(), GETDATE(), 1770361200),
(189, N'REAL20260207', N'T0012233009276130606245230499247964', CAST(N'2026-02-07T10:30:00.000' AS DateTime), N'Dritter Tag, deutlich besser.', 1, 1, 0, 0, 1, 0, 1, GETDATE(), GETDATE(), 1770450600),
(190, N'REAL20260208', N'T0012233009276130606245230499247964', CAST(N'2026-02-08T08:00:00.000' AS DateTime), N'Vorbei.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1770537600);

-- Mart 2026 (Ciklus nakon 29 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(191, N'REAL20260306', N'T0012233009276130606245230499247964', CAST(N'2026-03-06T08:00:00.000' AS DateTime), N'Starke Unterleibsschmerzen zum Start.', 1, 2, 2, 0, 2, 1, 2, GETDATE(), GETDATE(), 1772784000),
(192, N'REAL20260307', N'T0012233009276130606245230499247964', CAST(N'2026-03-07T09:00:00.000' AS DateTime), N'Etwas besser dank Schmerztablette.', 1, 2, 1, 0, 2, 0, 2, GETDATE(), GETDATE(), 1772870400),
(193, N'REAL20260308', N'T0012233009276130606245230499247964', CAST(N'2026-03-08T11:00:00.000' AS DateTime), N'Abklingend, nur noch leicht.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1772967600),
(194, N'REAL20260309', N'T0012233009276130606245230499247964', CAST(N'2026-03-09T08:00:00.000' AS DateTime), N'Sauber.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1773043200);

-- April 2026 (Ciklus nakon 28 dana)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(195, N'REAL20260403', N'T0012233009276130606245230499247964', CAST(N'2026-04-03T07:45:00.000' AS DateTime), N'Letzter Zyklus vor dem aktuellen Mai-Eintrag.', 1, 2, 2, 1, 1, 0, 2, GETDATE(), GETDATE(), 1775202300),
(196, N'REAL20260404', N'T0012233009276130606245230499247964', CAST(N'2026-04-04T09:00:00.000' AS DateTime), N'Tag 2 im April, normal verlaufend.', 1, 2, 1, 1, 2, 1, 2, GETDATE(), GETDATE(), 1775288400),
(197, N'REAL20260405', N'T0012233009276130606245230499247964', CAST(N'2026-04-05T12:00:00.000' AS DateTime), N'Nur noch ganz leicht.', 1, 1, 0, 0, 1, 0, 1, GETDATE(), GETDATE(), 1775390400),
(198, N'REAL20260406', N'T0012233009276130606245230499247964', CAST(N'2026-04-06T08:00:00.000' AS DateTime), N'Vorbei.', 1, 1, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1775462400);

-- Maj 2026 (Tekući ciklus nakon 30 dana - Zaključno sa današnjim datumom 20.05.2026.)
INSERT [dbuser_pfemme].[Cycles] ([ID], [UnixTS], [AuthUsers_UnixTS], [RecordDate], [Details], [bleeding], [intensity], [pain], [headache], [fatigue], [nausea], [cramps], [created_at], [updated_at], [LastUpdateUnixTS]) VALUES 
(199, N'REAL20260503', N'T0012233009276130606245230499247964', CAST(N'2026-05-03T08:00:00.000' AS DateTime), N'Aktueller Zyklus Mai gestartet.', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1777795200),
(200, N'REAL20260504', N'T0012233009276130606245230499247964', CAST(N'2026-05-04T09:15:00.000' AS DateTime), N'Krämpfe stärker am zweiten Tag.', 1, 2, 2, 1, 2, 0, 2, GETDATE(), GETDATE(), 1777886100),
(201, N'REAL20260505', N'T0012233009276130606245230499247964', CAST(N'2026-05-05T11:00:00.000' AS DateTime), N'Blutung lässt nach.', 1, 2, 1, 0, 1, 0, 1, GETDATE(), GETDATE(), 1777978800),
(202, N'REAL20260506', N'T0012233009276130606245230499247964', CAST(N'2026-05-06T08:00:00.000' AS DateTime), N'Letzter Tag der Blutung.', 1, 1, 0, 0, 1, 0, 0, GETDATE(), GETDATE(), 1778064000),
-- Trenutni unosi (Sredina maja - Ovulacija)
(203, N'REAL20260517', N'T0012233009276130606245230499247964', CAST(N'2026-05-17T14:00:00.000' AS DateTime), N'Ovulationstest positiv, leichtes Ziehen links.', 0, 0, 1, 0, 0, 0, 1, GETDATE(), GETDATE(), 1779026400),
(204, N'REAL20260520', N'T0012233009276130606245230499247964', CAST(N'2026-05-20T15:30:00.000' AS DateTime), N'Heute Eintrag: Keine Schmerzen, normale Stimmung.', 0, 0, 0, 0, 0, 0, 0, GETDATE(), GETDATE(), 1779291000);

GO

SET IDENTITY_INSERT [dbuser_pfemme].[Cycles] OFF
GO
