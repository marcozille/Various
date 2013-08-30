
// MFCApplication1Dlg.h : file di intestazione
//

#pragma once
#include "afxwin.h"

#include "MDVSEdit.h"

// finestra di dialogo CMFCApplication1Dlg
class CMFCApplication1Dlg : public CDialogEx
{
// Costruzione
public:
	CMFCApplication1Dlg(CWnd* pParent = NULL);	// costruttore standard

// Dati della finestra di dialogo
	enum { IDD = IDD_MFCAPPLICATION1_DIALOG };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// supporto DDX/DDV


// Implementazione
protected:
	HICON m_hIcon;

	HWND m_hWndHoveredControl;

	// Funzioni generate per la mappa dei messaggi
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
public:
	CMDVSEdit M_EDT;
	virtual LRESULT WindowProc(UINT message, WPARAM wParam, LPARAM lParam);
	LRESULT OnSetColors(HDC hDC, HWND hWnd);
	afx_msg BOOL OnEraseBkgnd(CDC* pDC);
	CMDVSEdit m_edt2;
	CMDVSEdit m_edt3;
	CMDVSEdit m_edt4;
	afx_msg void OnMove(int x, int y);
	afx_msg void OnMouseMove(UINT nFlags, CPoint point);
	virtual BOOL PreTranslateMessage(MSG* pMsg);
};
