# 3D Voxel Sand Simulator (Unity)

Интерактивная симуляция сыпучей среды (песка) в 3D на основе клеточного автомата.  
Оптимизирована для работы в реальном времени с помощью Unity Job System + Burst и Mesh Instancing.

## Screenshots

![Screenshot 1](Media/screenshot1.png)  
![Screenshot 2](Media/screenshot2.png)

## Features
- 3D воксельная сетка (до 200×200×200)
- Клеточный автомат: гравитация + диагональные смещения + боковое “скольжение” с вероятностью и учётом давления
- Управление источником песка (труба), регулировка потока
- Оптимизации:
  - отказ от миллионов GameObject → отрисовка через Mesh Instancing
  - параллельная симуляция через Unity Job System (IJobParallelFor) + Burst
  - хранение данных в NativeArray (Allocator.Persistent)
- Кастомный шейдер для визуального качества (процедурный шум + освещение)

## Controls
- Arrow keys — перемещение трубы
- Space — включить подачу песка
- Q — очистка/сброс

## Performance
**Tested on:** Ryzen 5 5600H, 16 GB RAM  
- 128×128×128: ~30 sim/s  
- 200×200×200: ~10 sim/s  

## Architecture (high-level)
- **SandSimulation** — управление циклом симуляции, данными и визуализацией
- **SandSimulationJob / ChunkSimulationWrapper** — параллельная обработка ячеек
- **PipeController** — генерация песчинок и управление потоком
- **Translator** — преобразование (x, y, z) ⇄ index

## How to run
1. Unity version: 2022.3 LTS
2. Open project in Unity
3. Open scene: `Assets/Scenes/SampleScene`
4. Play

## Roadmap
- Улучшить стабильность “гор” песка (уменьшить эрозию / изменить правила)
- Оптимизировать обновление visible voxels (частичный апдейт/dirty chunks)
- Экспорт настроек симуляции в UI (size, slip probability, seed)

## Tech stack
Unity, C#, Unity Collections (NativeArray), Job System, Burst, HLSL Shader

## Author
- GitHub: https://github.com/Gurov-Vyacheslav

## License
MIT
