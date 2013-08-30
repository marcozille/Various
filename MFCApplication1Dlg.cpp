
// MFCApplication1Dlg.cpp : file di implementazione
//

#include "stdafx.h"
#include "MFCApplication1.h"
#include "MFCApplication1Dlg.h"
#include "afxdialogex.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// finestra di dialogo CAboutDlg utilizzata per visualizzare le informazioni sull'applicazione.

class CAboutDlg : public CDialogEx
{
public:
	CAboutDlg();

// Dati della finestra di dialogo
	enum { IDD = IDD_ABOUTBOX };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // supporto DDX/DDV

// Implementazione
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialogEx(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)
END_MESSAGE_MAP()


// finestra di dialogo CMFCApplication1Dlg



CMFCApplication1Dlg::CMFCApplication1Dlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(CMFCApplication1Dlg::IDD, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	m_hWndHoveredControl = NULL;
}

void CMFCApplication1Dlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDIT1, M_EDT);
	DDX_Control(pDX, IDC_EDIT2, m_edt2);
	DDX_Control(pDX, IDC_EDIT4, m_edt3);
	DDX_Control(pDX, IDC_EDIT3, m_edt4);
}

BEGIN_MESSAGE_MAP(CMFCApplication1Dlg, CDialogEx)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_WM_ERASEBKGND()
	ON_WM_MOVE()
	ON_WM_MOUSEMOVE()
END_MESSAGE_MAP()


// gestori di messaggi di CMFCApplication1Dlg

BOOL CMFCApplication1Dlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	//M_EDT.ModifyStyleEx( 0, WS_EX_STATICEDGE, 0);

	// Aggiungere la voce di menu "Informazioni su..." al menu di sistema.

	// IDM_ABOUTBOX deve trovarsi tra i comandi di sistema.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		BOOL bNameValid;
		CString strAboutMenu;
		bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
		ASSERT(bNameValid);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Impostare l'icona per questa finestra di dialogo. Il framework non esegue questa operazione automaticamente
	//  se la finestra principale dell'applicazione non è una finestra di dialogo.
	SetIcon(m_hIcon, TRUE);			// Impostare icona grande.
	SetIcon(m_hIcon, FALSE);		// Impostare icona piccola.

	// TODO: aggiungere qui inizializzazione aggiuntiva.

	return TRUE;  // restituisce TRUE a meno che non venga impostato lo stato attivo su un controllo.
}

void CMFCApplication1Dlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialogEx::OnSysCommand(nID, lParam);
	}
}

// Se si aggiunge alla finestra di dialogo un pulsante di riduzione a icona, per trascinare l'icona sarà necessario
//  il codice sottostante. Per le applicazioni MFC che utilizzano il modello documento/visualizzazione,
//  questa operazione viene eseguita automaticamente dal framework.

void CMFCApplication1Dlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // contesto di dispositivo per il disegno

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Centrare l'icona nel rettangolo client.
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Disegnare l'icona
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

// Il sistema chiama questa funzione per ottenere la visualizzazione del cursore durante il trascinamento
//  della finestra ridotta a icona.
HCURSOR CMFCApplication1Dlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}



LRESULT CMFCApplication1Dlg::WindowProc(UINT message, WPARAM wParam, LPARAM lParam)
{
	switch(message)
	{
	case WM_CTLCOLOREDIT:
		//AfxMessageBox(_T("222"));
		return OnSetColors((HDC)wParam, (HWND)lParam);
	}
	return CDialogEx::WindowProc(message, wParam, lParam);
}


LRESULT CMFCApplication1Dlg::OnSetColors(HDC hDC, HWND hWnd)
{
	DWORD dwStyle = (DWORD)GetWindowLong(hWnd, GWL_STYLE);

	SetBkMode(hDC, OPAQUE);

	HWND focusedHwnd = ::GetFocus();
	HBRUSH retVal;

	if(hWnd == focusedHwnd)
	{
		SetBkColor(hDC, RGB(100,100,100));
		SetTextColor(hDC, RGB(255,255,255));
		retVal = CreateSolidBrush(RGB(100,100,100));
	}
	else if(m_hWndHoveredControl == hWnd)
	{
		SetBkColor(hDC, RGB(70,70,70));
		SetTextColor(hDC, RGB(255,255,255));
		retVal = CreateSolidBrush(RGB(70,70,70));
	}
	else
	{
		SetBkColor(hDC, RGB(60,60,60));
		SetTextColor(hDC, RGB(255,255,255));
		retVal = CreateSolidBrush(RGB(60,60,60));
	}

	return (LRESULT)retVal;
}

BOOL CMFCApplication1Dlg::OnEraseBkgnd(CDC* pDC)
{
	CRect clientRc;
	GetClientRect(clientRc);
	pDC->FillSolidRect(clientRc, RGB(45,45,48));
	return TRUE;
}


void CMFCApplication1Dlg::OnMove(int x, int y)
{
	CDialogEx::OnMove(x, y);
}


void CMFCApplication1Dlg::OnMouseMove(UINT nFlags, CPoint point)
{
	CDialogEx::OnMouseMove(nFlags, point);
}


BOOL CMFCApplication1Dlg::PreTranslateMessage(MSG* pMsg)
{
	if(pMsg->message == WM_MOUSEMOVE)
	{
		if(m_hWndHoveredControl != pMsg->hwnd)
		{
			CRect rc;
			::GetClientRect(m_hWndHoveredControl, rc);
			::InvalidateRect(m_hWndHoveredControl, rc, TRUE);
			m_hWndHoveredControl = pMsg->hwnd;
		}	
	}

	return CDialogEx::PreTranslateMessage(pMsg);
}
