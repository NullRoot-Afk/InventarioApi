using InventarioApi.Data;
using InventarioApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;

namespace InventarioApi.Controllers;

[ApiController]             //Indica que la clase es un controlador de API y habilita características específicas de API
[Authorize]
                            //como la validación automática de modelos y la inferencia de rutas.

[Route("api/[controller]")] //Define la ruta base para todas las acciones del controlador. 
                            //El token [controller] se reemplaza automáticamente con el nombre del controlador (sin el sufijo "Controller").
public class ProductosController : ControllerBase //Hereda de ControllerBase, proporciona funcionalidades básicas para manejar solicitudes HTTP
                                                  //y devolver respuestas (o).
{
    private readonly AppDbContext _context;/*
                                                Campo privado de solo lectura que almacena la instancia del contexto de la base de datos.
                                                Se utiliza para interactuar con la base de datos a través de Entity Framework Core.
                                           */

    public ProductosController(AppDbContext context)/*
                                                     * Constructor del controlador que recibe una instancia de AppDbContext a través de inyección de dependencias.
                                                       Esta instancia se asigna al campo privado _context para su uso en las acciones del controlador.
                                                       Este metodo solo es utilizado por el framework de ASP.NET Core para crear instancias del controlador y no debe ser llamado directamente.
                                                    */
    {
        _context = context;
    }

    // GET: api/productos

    [HttpGet]//indicador de que este metodo responde a solicitudes HTTP GET

    
    [ProducesResponseType(typeof(IEnumerable<Producto>), StatusCodes.Status200OK)]//Indica que la acción puede devolver un resultado HTTP 200 OK con una colección de productos
                                                                                  //en el cuerpo de la respuesta.

    /*  Producto: representa un producto individual.
        IEnumerable<Producto>: representa una colección de productos que se pueden recorrer.
        ActionResult<T>: permite devolver un resultado HTTP o un valor del tipo T.
        Task<T>: representa una operación que terminará en el futuro.
        async: permite utilizar await dentro del método.*/
    public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()//metodo asincrono que devuelve una lista de productos.
                                                                         //El método devuelve un ActionResult que contiene una colección de productos.
    {
        return await _context.Productos //Accede a la tabla productos almacenada en el contexto de la base de datos
                                        .AsNoTracking() //Indica a EF Core que no realice seguimiento de cambios, esto mejora el rendimiento en operaciones de solo lectura
                                        .ToListAsync(); //Ejecuta la consulta de manera asincrónica y devuelve una lista de productos.
    }

    // GET: api/productos/5
    [HttpGet("{id:int}")]//Indica que este metodo responde a solicitudes HTTP GET con un parámetro de ruta que es un entero (id).
    [ProducesResponseType(typeof(Producto), StatusCodes.Status200OK)] //indica que la accion puede devolver 202 ok con un producto en el cuerpo de la respuesta
    [ProducesResponseType(StatusCodes.Status404NotFound)] //indica que la accion puede devolver 404 not found cuando el id no coincide con ningun producto en la base de datos
    public async Task<ActionResult<Producto>> GetProducto(int id)
    {
        var producto = await _context.Productos.FindAsync(id);//busca el producto por id en la base de datos de manera asincrona y lo almacena en la variable producto

        if (producto is null) //Si el producto no fue encontrado retorna http 404 not found
            return NotFound();

        return producto;//Si el producto fue encontrado retorna el producto con http 200 ok
    }

    // POST: api/productos
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(Producto), StatusCodes.Status201Created)]//respuesta HTTP 201 Created con el producto creado en el cuerpo de la respuesta
    [ProducesResponseType(StatusCodes.Status400BadRequest)]//la accion puede devolver 400 bad request cuando la peticion no es valida
    [ProducesResponseType(StatusCodes.Status409Conflict)] //la accion puede devolver 409 conflict cuando el codigo SKU en la peticion ya existe en la base de datos
    public async Task<ActionResult<Producto>> PostProducto(Producto producto)
    {
        producto.Id = 0; // Asegura que el Id sea 0 para que EF Core lo genere automáticamente al insertar un nuevo registro.

        _context.Productos.Add(producto);//Agrega el producto al contexto de la base de datos, marcándolo para su inserción.    

        try
        {
            await _context.SaveChangesAsync();//Guarda los cambios en la base de datos de manera asincrona, si el SKU ya existe en la bd 
                                              //lanzara una excepcion DbUpdateException que sera capturada en el catch
        }
        catch (DbUpdateException)
        {
            if (await _context.Productos.AnyAsync(p => p.CodigoSKU == producto.CodigoSKU)) //busca si existe el sku en la bd, si es asi retorna 409 conflict con un mensaje de error
                return Conflict($"Ya existe un producto con el CodigoSKU '{producto.CodigoSKU}'.");
            throw; //
        }

        return CreatedAtAction(nameof(GetProducto), new { id = producto.Id }, producto); //Si el producto fue creado exitosamente, retorna HTTP 201

    }

    // PUT: api/productos/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PutProducto(int id, Producto producto)
    {
        if (id != producto.Id)
            return BadRequest("El Id de la URL no coincide con el Id del cuerpo.");

        var existente = await _context.Productos.FindAsync(id);//busca el producto por id, si no lo encuentra devuelve 404 not found
        if (existente is null)
            return NotFound();
        //si pasa los dos checks anteriores, actualiza las propiedades del producto existente con los valores del producto recibido en la solicitud
        existente.Nombre = producto.Nombre;
        existente.CodigoSKU = producto.CodigoSKU;
        existente.Precio = producto.Precio;
        existente.Stock = producto.Stock;

        //la evaluacion del sku se hace para evitar que se actualice el producto con un sku que ya existe en otro producto,
        //si es asi retorna 409 conflict con un mensaje de error
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)//
        {
            if (await _context.Productos.AnyAsync(p => p.CodigoSKU == producto.CodigoSKU && p.Id != id))//busca si existe otro producto con el mismo SKU pero diferente Id
                return Conflict($"Ya existe otro producto con el CodigoSKU '{producto.CodigoSKU}'.");
            throw;
        }

        return NoContent();
    }

    // DELETE: api/productos/5
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProducto(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if (producto is null)
            return NotFound();

        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
